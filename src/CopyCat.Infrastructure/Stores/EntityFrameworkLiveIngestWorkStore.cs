using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Npgsql;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkLiveIngestWorkStore(CopyCatDbContext dbContext) : ILiveIngestWorkStore
{
    public async Task<IReadOnlyList<ChannelMapping>> GetLiveMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .Where(x => x.IsEnabled && x.LiveForwardingEnabled)
            .OrderBy(x => x.SourceChannelId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<ChannelSyncState> GetOrCreateSyncStateAsync(
        Guid sessionId,
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        ChannelSyncState? syncState = await dbContext.ChannelSyncStates.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.ChannelId == channelId,
            cancellationToken);
        if (syncState is not null)
        {
            return syncState;
        }

        syncState = new ChannelSyncState { SessionId = sessionId, ChannelId = channelId };
        dbContext.ChannelSyncStates.Add(syncState);
        return syncState;
    }

    public async Task<StoredMessage> GetOrStoreMessageAsync(
        StoredMessage candidate,
        CancellationToken cancellationToken = default)
    {
        StoredMessage? existing = await dbContext.Messages
            .Include(x => x.Attachments)
            .Include(x => x.Links)
            .FirstOrDefaultAsync(
                x => x.SourceChannelId == candidate.SourceChannelId &&
                     x.TelegramMessageId == candidate.TelegramMessageId,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        dbContext.Messages.Add(candidate);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return candidate;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            EntityEntry entry = dbContext.Entry(candidate);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }

            return await dbContext.Messages
                .Include(x => x.Attachments)
                .Include(x => x.Links)
                .FirstAsync(
                    x => x.SourceChannelId == candidate.SourceChannelId &&
                         x.TelegramMessageId == candidate.TelegramMessageId,
                    cancellationToken);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
