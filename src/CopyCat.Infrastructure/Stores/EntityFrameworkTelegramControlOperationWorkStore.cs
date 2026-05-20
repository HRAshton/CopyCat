using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkTelegramControlOperationWorkStore(CopyCatDbContext dbContext)
    : ITelegramControlOperationWorkStore
{
    public async Task<TelegramControlOperation?> GetNextPendingAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return await dbContext.TelegramControlOperations
            .Where(x => x.Status == TelegramControlOperationStatus.Pending
                        && (x.NextRetryAt == null || x.NextRetryAt <= now))
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ResetStuckOperationsAsync(
        DateTimeOffset stuckBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        List<TelegramControlOperation> stuck = await dbContext.TelegramControlOperations
            .Where(x => x.Status == TelegramControlOperationStatus.Processing
                        && x.StartedAt != null
                        && x.StartedAt < stuckBeforeUtc)
            .ToListAsync(cancellationToken);

        foreach (TelegramControlOperation op in stuck)
        {
            op.Status = TelegramControlOperationStatus.Pending;
            op.StartedAt = null;
        }

        if (stuck.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<TelegramChannel> GetChannelAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramChannels.FirstAsync(x => x.Id == channelId, cancellationToken);
    }

    public async Task<ChannelMapping> GetMappingWithSourceChannelAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    public async Task UpsertDiscoveredChannelsAsync(
        Guid sessionId,
        IReadOnlyList<TelegramChannel> channels,
        CancellationToken cancellationToken = default)
    {
        foreach (TelegramChannel channel in channels)
        {
            TelegramChannel? existing = await dbContext.TelegramChannels.FirstOrDefaultAsync(
                x => x.SessionId == sessionId && x.TelegramChannelId == channel.TelegramChannelId,
                cancellationToken);
            if (existing is null)
            {
                dbContext.TelegramChannels.Add(channel);
                continue;
            }

            existing.Title = channel.Title;
            existing.Username = channel.Username;
            existing.ChannelType = channel.ChannelType;
            existing.CanPost = channel.CanPost;
            existing.CanAdmin = channel.CanAdmin;
            existing.CanCreateRelatedTargets = channel.CanCreateRelatedTargets;
            existing.RawJson = channel.RawJson;
            existing.DiscoveredAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task<TelegramChannel> UpsertTargetChannelAsync(
        Guid sessionId,
        TelegramChannel target,
        CancellationToken cancellationToken = default)
    {
        target.SessionId = sessionId;
        target.IsTarget = true;

        TelegramChannel? existing = await dbContext.TelegramChannels.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.TelegramChannelId == target.TelegramChannelId,
            cancellationToken);
        if (existing is null)
        {
            dbContext.TelegramChannels.Add(target);
            return target;
        }

        existing.Title = target.Title;
        existing.Username = target.Username;
        existing.IsTarget = true;
        existing.CanPost = target.CanPost;
        existing.CanAdmin = target.CanAdmin;
        existing.CanCreateRelatedTargets = target.CanCreateRelatedTargets;
        existing.RawJson = target.RawJson;
        existing.DiscoveredAt = DateTimeOffset.UtcNow;
        return existing;
    }

    public async Task<int> InsertMessagesIfMissingAsync(
        IReadOnlyList<StoredMessage> messages,
        CancellationToken cancellationToken = default)
    {
        int inserted = 0;
        foreach (StoredMessage message in messages)
        {
            bool exists = await dbContext.Messages.AnyAsync(
                x => x.SourceChannelId == message.SourceChannelId && x.TelegramMessageId == message.TelegramMessageId,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.Messages.Add(message);
            inserted += 1;
        }

        return inserted;
    }

    public async Task UpdateBackfillSyncStateAsync(
        Guid sessionId,
        Guid channelId,
        long? lastBackfilledMessageId,
        CancellationToken cancellationToken = default)
    {
        ChannelSyncState? syncState = await dbContext.ChannelSyncStates.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.ChannelId == channelId,
            cancellationToken);
        if (syncState is null)
        {
            syncState = new ChannelSyncState { SessionId = sessionId, ChannelId = channelId };
            dbContext.ChannelSyncStates.Add(syncState);
        }

        syncState.LastBackfilledMessageId = lastBackfilledMessageId;
        syncState.LastSyncAt = DateTimeOffset.UtcNow;
        syncState.SyncStatus = ChannelSyncStatus.Backfilled;
        syncState.LastError = null;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
