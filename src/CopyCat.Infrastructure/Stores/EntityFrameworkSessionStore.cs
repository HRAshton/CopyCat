using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkSessionStore(CopyCatDbContext dbContext) : ISessionStore
{
    public async Task<IReadOnlyList<TelegramSession>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramSessions
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TelegramSession> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task AddAsync(TelegramSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.TelegramSessions.AddAsync(session, cancellationToken);
    }

    public async Task<bool> HasDependentsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        bool hasChannels = await dbContext.TelegramChannels.AnyAsync(x => x.SessionId == sessionId, cancellationToken);
        bool hasMessages = await dbContext.Messages.AnyAsync(x => x.SessionId == sessionId, cancellationToken);
        return hasChannels || hasMessages;
    }

    public void Remove(TelegramSession session)
    {
        dbContext.TelegramSessions.Remove(session);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
