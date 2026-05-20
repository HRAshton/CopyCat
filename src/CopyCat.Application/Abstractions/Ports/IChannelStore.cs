using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides persistence operations for Telegram channels and their owning sessions.
/// </summary>
public interface IChannelStore
{
    /// <summary>
    /// Loads all known Telegram channels.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All persisted Telegram channels.</returns>
    Task<IReadOnlyList<TelegramChannel>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a channel by identifier.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested Telegram channel.</returns>
    Task<TelegramChannel> GetAsync(Guid channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all connected and enabled sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All sessions that are both enabled and currently connected.</returns>
    Task<IReadOnlyList<TelegramSession>> GetConnectedSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the session that owns the specified channel.
    /// </summary>
    /// <param name="channelId">The identifier of the channel whose owning session should be loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The Telegram session that owns the channel.</returns>
    Task<TelegramSession> GetOwningSessionAsync(Guid channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the channel still has dependent mappings or history.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when the channel still participates in related data; otherwise, <see langword="false"/>.</returns>
    Task<bool> HasDependentsAsync(Guid channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a channel from persistence.
    /// </summary>
    /// <param name="channel">The tracked channel entity to remove.</param>
    void Remove(TelegramChannel channel);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
