using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides persistence operations for rewrite sets and their versions.
/// </summary>
public interface IRewriteSetStore
{
    /// <summary>
    /// Loads all rewrite sets with their versions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All persisted rewrite sets with their version history.</returns>
    Task<IReadOnlyList<RewriteSet>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a rewrite set with its versions.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested rewrite set with its versions.</returns>
    Task<RewriteSet> GetAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new rewrite set instance and begins tracking it.
    /// </summary>
    /// <returns>A new tracked rewrite set entity.</returns>
    RewriteSet Create();

    /// <summary>
    /// Calculates the next version number for the specified rewrite set.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set whose next version number is needed.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The next sequential version number for the rewrite set.</returns>
    Task<int> GetNextVersionNumberAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new rewrite version to persistence.
    /// </summary>
    /// <param name="version">The rewrite version entity to begin tracking.</param>
    void AddVersion(RewriteVersion version);

    /// <summary>
    /// Loads the latest published rewrite version across all sets.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The most recently published rewrite version, or <see langword="null"/> when none exist.</returns>
    Task<RewriteVersion?> GetLatestPublishedVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a rewrite set is still attached to mappings.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when at least one mapping still references the rewrite set; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInUseAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all versions belonging to a rewrite set.
    /// </summary>
    /// <param name="versions">The rewrite versions to remove.</param>
    void RemoveVersions(IEnumerable<RewriteVersion> versions);

    /// <summary>
    /// Removes a rewrite set.
    /// </summary>
    /// <param name="rewriteSet">The tracked rewrite set entity to remove.</param>
    void Remove(RewriteSet rewriteSet);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
