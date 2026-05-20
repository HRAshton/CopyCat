using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides persistence operations for filter sets and their versions.
/// </summary>
public interface IFilterSetStore
{
    /// <summary>
    /// Loads all filter sets with their versions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All persisted filter sets with their version history.</returns>
    Task<IReadOnlyList<FilterSet>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a filter set with its versions.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested filter set with its versions.</returns>
    Task<FilterSet> GetAsync(Guid filterSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new filter set instance and begins tracking it.
    /// </summary>
    /// <returns>A new tracked filter set entity.</returns>
    FilterSet Create();

    /// <summary>
    /// Calculates the next version number for the specified filter set.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set whose next version number is needed.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The next sequential version number for the filter set.</returns>
    Task<int> GetNextVersionNumberAsync(Guid filterSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new filter version to persistence.
    /// </summary>
    /// <param name="version">The filter version entity to begin tracking.</param>
    void AddVersion(FilterVersion version);

    /// <summary>
    /// Loads recent stored messages for debugging.
    /// </summary>
    /// <param name="channelId">An optional source channel filter; when omitted, messages from all channels are considered.</param>
    /// <param name="take">The maximum number of recent messages to return.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The most recent stored messages for debugging filter behavior.</returns>
    Task<IReadOnlyList<StoredMessage>> GetRecentMessagesAsync(
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a filter set is still attached to mappings.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when at least one mapping still references the filter set; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInUseAsync(Guid filterSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all versions belonging to a filter set.
    /// </summary>
    /// <param name="versions">The filter versions to remove.</param>
    void RemoveVersions(IEnumerable<FilterVersion> versions);

    /// <summary>
    /// Removes a filter set.
    /// </summary>
    /// <param name="filterSet">The tracked filter set entity to remove.</param>
    void Remove(FilterSet filterSet);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
