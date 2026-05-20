using CopyCat.Application.Abstractions;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Persists long-running interactive Telegram login outcomes back to the database.
/// </summary>
public sealed class TelegramInteractiveLoginSink(IServiceScopeFactory scopeFactory) : ITelegramInteractiveLoginSink
{
    /// <summary>
    /// Marks an interactive login as completed and stores the connected session payload.
    /// </summary>
    /// <param name="sessionId">The identifier of the Telegram session that completed login.</param>
    /// <param name="sessionDataEncrypted">The encrypted Telegram session payload returned by the login flow.</param>
    /// <param name="connectedAt">The UTC timestamp when the session became connected.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes after the session has been updated, or immediately if it no longer exists.</returns>
    public async Task CompleteLoginAsync(
        Guid sessionId,
        string sessionDataEncrypted,
        DateTimeOffset connectedAt,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        CopyCatDbContext dbContext = scope.ServiceProvider.GetRequiredService<CopyCatDbContext>();
        TelegramSession? session = await dbContext.TelegramSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.MarkConnected(sessionDataEncrypted, connectedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks an interactive login as failed and stores the resulting error state on the session.
    /// </summary>
    /// <param name="sessionId">The identifier of the Telegram session that failed login.</param>
    /// <param name="errorMessage">The failure message returned by the login flow.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes after the session has been updated, or immediately if it no longer exists.</returns>
    public async Task FailLoginAsync(Guid sessionId, string errorMessage, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        CopyCatDbContext dbContext = scope.ServiceProvider.GetRequiredService<CopyCatDbContext>();
        TelegramSession? session = await dbContext.TelegramSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.MarkFaulted(errorMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
