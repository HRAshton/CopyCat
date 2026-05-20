using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Services;

/// <summary>
/// Manages Telegram channels: discovery, source/target designation, and deletion.
/// </summary>
internal sealed class ChannelService(
    IChannelStore channelStore,
    ITelegramControlOperationScheduler controlOperationScheduler,
    IAuditLogService auditLogService) : IChannelManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ChannelSummary>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TelegramChannel> channels = await channelStore.ListAsync(cancellationToken);
        return channels
            .OrderByDescending(x => x.DiscoveredAt)
            .Select(x => new ChannelSummary(
                x.Id,
                x.SessionId,
                x.Title,
                x.Username,
                x.ChannelType,
                x.IsSource,
                x.IsTarget,
                x.CanPost,
                x.CanAdmin,
                x.DiscoveredAt))
            .ToList();
    }

    /// <inheritdoc />
    public async Task DiscoverChannelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TelegramSession> sessions = await channelStore.GetConnectedSessionsAsync(cancellationToken);
        if (sessions.Count == 0)
        {
            throw new InvalidDomainOperationException(
                "No connected Telegram sessions are available. Complete session login first.");
        }

        foreach (TelegramSession session in sessions)
        {
            if (await controlOperationScheduler.HasPendingAsync(
                    session.Id,
                    TelegramControlOperationType.DiscoverChannels,
                    cancellationToken))
            {
                continue;
            }

            await controlOperationScheduler.EnqueueAsync(
                new TelegramControlOperation
                {
                    SessionId = session.Id,
                    OperationType = TelegramControlOperationType.DiscoverChannels,
                },
                cancellationToken);
        }

        await auditLogService.WriteAsync(
            "channel.discovery-queued",
            nameof(TelegramControlOperation),
            null,
            null,
            new { SessionCount = sessions.Count },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetSourceStateAsync(Guid channelId, bool isSource, CancellationToken cancellationToken = default)
    {
        TelegramChannel channel = await channelStore.GetAsync(channelId, cancellationToken);
        channel.IsSource = isSource;
        await channelStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "channel.source-toggled",
            nameof(TelegramChannel),
            channel.Id,
            null,
            channel,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetTargetStateAsync(Guid channelId, bool isTarget, CancellationToken cancellationToken = default)
    {
        TelegramChannel channel = await channelStore.GetAsync(channelId, cancellationToken);
        channel.IsTarget = isTarget;
        await channelStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "channel.target-toggled",
            nameof(TelegramChannel),
            channel.Id,
            null,
            channel,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreateTargetForSourceAsync(
        Guid sourceChannelId,
        string title,
        CancellationToken cancellationToken = default)
    {
        TelegramSession session = await channelStore.GetOwningSessionAsync(sourceChannelId, cancellationToken);
        if (session.Status != TelegramSessionStatus.Connected)
        {
            throw new InvalidDomainOperationException("The source session is not connected.");
        }

        await controlOperationScheduler.EnqueueAsync(
            new TelegramControlOperation
            {
                SessionId = session.Id,
                SourceChannelId = sourceChannelId,
                OperationType = TelegramControlOperationType.CreateTargetChannel,
                PayloadJson = JsonSerializer.Serialize(new Models.CreateTargetChannelPayload(title.Trim())),
            },
            cancellationToken);
        await auditLogService.WriteAsync(
            "channel.target-create-queued",
            nameof(TelegramControlOperation),
            null,
            null,
            new { sourceChannelId, title },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteChannelAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        TelegramChannel channel = await channelStore.GetAsync(channelId, cancellationToken);
        if (await channelStore.HasDependentsAsync(channelId, cancellationToken))
        {
            throw new InvalidDomainOperationException(
                "Only unused channels can be deleted. Remove dependent mappings and message history first.");
        }

        channelStore.Remove(channel);
        await channelStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "channel.deleted",
            nameof(TelegramChannel),
            channelId,
            null,
            new { Id = channelId },
            cancellationToken);
    }
}
