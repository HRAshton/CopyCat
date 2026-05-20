using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides data access for Telegram control-operation processing.
/// </summary>
public interface ITelegramControlOperationWorkStore
{
    /// <summary>
    /// Loads the next pending control operation, if any.
    /// Operations whose <c>NextRetryAt</c> is in the future are excluded.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The next control operation ready to run, or <see langword="null"/> when none are available.</returns>
    Task<TelegramControlOperation?> GetNextPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets any operations that have been stuck in <c>Processing</c> state
    /// since before <paramref name="stuckBeforeUtc"/> back to <c>Pending</c>.
    /// </summary>
    /// <param name="stuckBeforeUtc">The cutoff timestamp used to identify stale processing operations.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>A task that completes when stale operations have been reset.</returns>
    Task ResetStuckOperationsAsync(DateTimeOffset stuckBeforeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a Telegram session by identifier.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested Telegram session.</returns>
    Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a channel by identifier.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested Telegram channel.</returns>
    Task<TelegramChannel> GetChannelAsync(Guid channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a mapping with its source channel.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested mapping with its source channel populated.</returns>
    Task<ChannelMapping> GetMappingWithSourceChannelAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts discovered channels for a session.
    /// </summary>
    /// <param name="sessionId">The session the discovered channels belong to.</param>
    /// <param name="channels">The discovered channel snapshot to merge into persistence.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the discovered channels have been merged.</returns>
    Task UpsertDiscoveredChannelsAsync(
        Guid sessionId,
        IReadOnlyList<TelegramChannel> channels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a created target channel for a session.
    /// </summary>
    /// <param name="sessionId">The session that owns the new target channel.</param>
    /// <param name="target">The target channel snapshot returned by Telegram.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>The persisted target channel entity.</returns>
    Task<TelegramChannel> UpsertTargetChannelAsync(
        Guid sessionId,
        TelegramChannel target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts new stored messages and returns the number inserted.
    /// </summary>
    /// <param name="messages">The stored messages to insert when they do not already exist.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>The number of new messages inserted.</returns>
    Task<int> InsertMessagesIfMissingAsync(
        IReadOnlyList<StoredMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates backfill synchronization state.
    /// </summary>
    /// <param name="sessionId">The session that owns the sync state.</param>
    /// <param name="channelId">The source channel whose sync state should be updated.</param>
    /// <param name="lastBackfilledMessageId">The newest Telegram message identifier included in the completed backfill, if any.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the sync state has been updated.</returns>
    Task UpdateBackfillSyncStateAsync(
        Guid sessionId,
        Guid channelId,
        long? lastBackfilledMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
