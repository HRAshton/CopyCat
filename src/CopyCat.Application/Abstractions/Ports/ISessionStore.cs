using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides persistence operations for Telegram sessions.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Loads all sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All persisted Telegram sessions.</returns>
    Task<IReadOnlyList<TelegramSession>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a session by identifier.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested Telegram session.</returns>
    Task<TelegramSession> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new session to persistence.
    /// </summary>
    /// <param name="session">The session entity to add.</param>
    /// <param name="cancellationToken">The cancellation token for the add operation.</param>
    /// <returns>A task that completes when the session has been registered with the underlying store.</returns>
    Task AddAsync(TelegramSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a session still has dependent channels or message history.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when the session still owns related data; otherwise, <see langword="false"/>.</returns>
    Task<bool> HasDependentsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a session from persistence.
    /// </summary>
    /// <param name="session">The tracked session entity to remove.</param>
    void Remove(TelegramSession session);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
