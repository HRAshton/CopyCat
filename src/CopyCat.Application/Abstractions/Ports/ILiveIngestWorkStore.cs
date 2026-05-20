using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides data access for live-ingest batches.
/// </summary>
public interface ILiveIngestWorkStore
{
    /// <summary>
    /// Loads enabled mappings that have live forwarding turned on.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All enabled mappings configured for live ingest.</returns>
    Task<IReadOnlyList<ChannelMapping>> GetLiveMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a Telegram session by identifier.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested Telegram session.</returns>
    Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads or creates channel sync state for the given session/channel pair.
    /// </summary>
    /// <param name="sessionId">The session that owns the sync state.</param>
    /// <param name="channelId">The channel whose sync state is needed.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The existing or newly created sync state entity.</returns>
    Task<ChannelSyncState> GetOrCreateSyncStateAsync(
        Guid sessionId,
        Guid channelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a message when it does not exist yet, or returns the already stored copy.
    /// </summary>
    /// <param name="candidate">The message candidate produced by Telegram ingestion.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The newly stored message, or the existing stored copy when the message was already present.</returns>
    Task<StoredMessage> GetOrStoreMessageAsync(
        StoredMessage candidate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
