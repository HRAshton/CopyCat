using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkChannelStore(CopyCatDbContext dbContext) : IChannelStore
{
    public async Task<IReadOnlyList<TelegramChannel>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramChannels
            .OrderByDescending(x => x.DiscoveredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TelegramChannel> GetAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramChannels.FirstAsync(x => x.Id == channelId, cancellationToken);
    }

    public async Task<IReadOnlyList<TelegramSession>> GetConnectedSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramSessions
            .Where(x => x.IsEnabled && x.Status == TelegramSessionStatus.Connected)
            .ToListAsync(cancellationToken);
    }

    public async Task<TelegramSession> GetOwningSessionAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        Guid sessionId = await dbContext.TelegramChannels
            .Where(x => x.Id == channelId)
            .Select(x => x.SessionId)
            .FirstAsync(cancellationToken);

        return await dbContext.TelegramSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<bool> HasDependentsAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        bool usedByMappings = await dbContext.ChannelMappings
            .AnyAsync(x => x.SourceChannelId == channelId || x.TargetChannelId == channelId, cancellationToken);
        bool hasMessages = await dbContext.Messages.AnyAsync(x => x.SourceChannelId == channelId, cancellationToken);
        return usedByMappings || hasMessages;
    }

    public void Remove(TelegramChannel channel)
    {
        dbContext.TelegramChannels.Remove(channel);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
