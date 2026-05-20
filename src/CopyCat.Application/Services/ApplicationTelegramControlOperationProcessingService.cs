using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Models;
using CopyCat.Application.Options;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

using Microsoft.Extensions.Options;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates queued Telegram control-operation processing.
/// </summary>
internal sealed class ApplicationTelegramControlOperationProcessingService(
    ITelegramControlOperationWorkStore controlOperationWorkStore,
    ITelegramGateway telegramGateway,
    IOptions<ApplicationWorkerOptions> options) : ITelegramControlOperationProcessingService
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Operation failures should not stop the worker loop.")]
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        // Reset any stuck operations before picking up new work.
        await ResetStuckOperationsAsync(cancellationToken);

        TelegramControlOperation? operation = await controlOperationWorkStore.GetNextPendingAsync(cancellationToken);
        if (operation is null)
        {
            return false;
        }

        operation.Begin();
        await controlOperationWorkStore.SaveChangesAsync(cancellationToken);

        try
        {
            string resultJson = operation.OperationType switch
            {
                TelegramControlOperationType.DiscoverChannels => await DiscoverChannelsAsync(
                    operation,
                    cancellationToken),
                TelegramControlOperationType.CreateTargetChannel => await CreateTargetChannelAsync(
                    operation,
                    cancellationToken),
                TelegramControlOperationType.RunBackfill => await RunBackfillAsync(operation, cancellationToken),
                _ => throw new InvalidOperationException(
                    $"Unsupported control operation type '{operation.OperationType}'."),
            };
            operation.Complete(resultJson);
        }
        catch (Exception exception)
        {
            operation.RecordRetry(exception.Message, options.Value.ControlOperationRetryDelay);
        }

        await controlOperationWorkStore.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    private static void EnsureConnected(TelegramSession session, string message)
    {
        if (!session.IsEnabled || session.Status != TelegramSessionStatus.Connected)
        {
            throw new InvalidOperationException(message);
        }
    }

    private async Task ResetStuckOperationsAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset stuckThreshold = DateTimeOffset.UtcNow.Subtract(options.Value.ControlOperationStuckThreshold);
        await controlOperationWorkStore.ResetStuckOperationsAsync(stuckThreshold, cancellationToken);
    }

    private async Task<string> DiscoverChannelsAsync(
        TelegramControlOperation operation,
        CancellationToken cancellationToken)
    {
        Guid sessionId = operation.SessionId
                         ?? throw new InvalidOperationException("DiscoverChannels requires SessionId.");
        TelegramSession session = await controlOperationWorkStore.GetSessionAsync(sessionId, cancellationToken);
        EnsureConnected(session, "The session is not connected.");
        IReadOnlyList<TelegramChannel> channels =
            await telegramGateway.DiscoverChannelsAsync(session, cancellationToken);
        await controlOperationWorkStore.UpsertDiscoveredChannelsAsync(session.Id, channels, cancellationToken);
        return JsonSerializer.Serialize(new { Discovered = channels.Count }, CreateJsonOptions());
    }

    private async Task<string> CreateTargetChannelAsync(
        TelegramControlOperation operation,
        CancellationToken cancellationToken)
    {
        Guid sourceChannelId = operation.SourceChannelId
                               ?? throw new InvalidOperationException("CreateTargetChannel requires SourceChannelId.");
        TelegramChannel source = await controlOperationWorkStore.GetChannelAsync(sourceChannelId, cancellationToken);
        TelegramSession session = await controlOperationWorkStore.GetSessionAsync(source.SessionId, cancellationToken);
        EnsureConnected(session, "The source session is not connected.");
        CreateTargetChannelPayload payload =
            JsonSerializer.Deserialize<CreateTargetChannelPayload>(
                operation.PayloadJson ?? string.Empty,
                CreateJsonOptions())
            ?? throw new InvalidOperationException("CreateTargetChannel payload was missing.");
        TelegramChannel target =
            await telegramGateway.CreateTargetChannelAsync(session, payload.Title, cancellationToken);
        TelegramChannel persisted = await controlOperationWorkStore.UpsertTargetChannelAsync(
            session.Id,
            target,
            cancellationToken);
        return JsonSerializer.Serialize(new { TargetChannelId = persisted.Id, persisted.Title }, CreateJsonOptions());
    }

    private async Task<string> RunBackfillAsync(
        TelegramControlOperation operation,
        CancellationToken cancellationToken)
    {
        Guid mappingId = operation.MappingId
                         ?? throw new InvalidOperationException("RunBackfill requires MappingId.");
        ChannelMapping mapping =
            await controlOperationWorkStore.GetMappingWithSourceChannelAsync(mappingId, cancellationToken);
        TelegramSession session = await controlOperationWorkStore.GetSessionAsync(
            mapping.SourceChannel.SessionId,
            cancellationToken);
        EnsureConnected(session, "The source session is not connected.");
        RunBackfillPayload payload =
            JsonSerializer.Deserialize<RunBackfillPayload>(
                operation.PayloadJson ?? string.Empty,
                CreateJsonOptions())
            ?? throw new InvalidOperationException("RunBackfill payload was missing.");
        IReadOnlyList<StoredMessage> messages = await telegramGateway.BackfillMessagesAsync(
            session,
            mapping.SourceChannel,
            payload.Take,
            cancellationToken: cancellationToken);
        int inserted = await controlOperationWorkStore.InsertMessagesIfMissingAsync(messages, cancellationToken);
        long? lastBackfilledMessageId = messages
            .OrderByDescending(x => x.TelegramMessageId)
            .Select(x => (long?)x.TelegramMessageId)
            .FirstOrDefault();
        await controlOperationWorkStore.UpdateBackfillSyncStateAsync(
            session.Id,
            mapping.SourceChannelId,
            lastBackfilledMessageId,
            cancellationToken);
        return JsonSerializer.Serialize(new { Requested = payload.Take, Inserted = inserted }, CreateJsonOptions());
    }
}
