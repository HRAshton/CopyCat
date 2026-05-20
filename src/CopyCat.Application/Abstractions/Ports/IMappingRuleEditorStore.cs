using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides the data-access operations needed by the mapping rule editor use case.
/// </summary>
public interface IMappingRuleEditorStore
{
    // -- Mapping load variants -------------------------------------------------

    /// <summary>
    /// Loads the mapping with its active filter set and version history.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The mapping with its active filter set graph.</returns>
    Task<ChannelMapping> GetMappingWithFilterSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the mapping with its source/target channels and active filter set with version history.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The mapping with channel and active filter set details.</returns>
    Task<ChannelMapping> GetMappingWithChannelsAndFilterSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the mapping with its active rewrite set and version history.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The mapping with its active rewrite set graph.</returns>
    Task<ChannelMapping> GetMappingWithRewriteSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the mapping with its source/target channels and active rewrite set with version history.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The mapping with channel and active rewrite set details.</returns>
    Task<ChannelMapping> GetMappingWithChannelsAndRewriteSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the mapping without navigation properties.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The bare mapping entity.</returns>
    Task<ChannelMapping> GetMappingAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    // -- Message loading -------------------------------------------------------

    /// <summary>
    /// Returns the most recent stored messages from the specified source channel.
    /// </summary>
    /// <param name="sourceChannelId">The source channel whose messages should be returned.</param>
    /// <param name="take">The maximum number of recent messages to return.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The newest stored messages for the source channel.</returns>
    Task<IReadOnlyList<StoredMessage>> GetRecentSourceMessagesAsync(
        Guid sourceChannelId,
        int take,
        CancellationToken cancellationToken = default);

    // -- Filter set mutations --------------------------------------------------

    /// <summary>
    /// Registers a new filter set with the change tracker.
    /// </summary>
    /// <param name="filterSet">The filter set entity to begin tracking.</param>
    void AddFilterSet(FilterSet filterSet);

    /// <summary>
    /// Registers a new filter version with the change tracker.
    /// </summary>
    /// <param name="version">The filter version entity to begin tracking.</param>
    void AddFilterVersion(FilterVersion version);

    /// <summary>
    /// Returns <c>true</c> if any mapping still references the specified filter set.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when a mapping still references the filter set; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsFilterSetReferencedAsync(
        Guid filterSetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the filter set with its versions, or <c>null</c> if it does not exist.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The filter set with its versions, or <see langword="null"/> when it does not exist.</returns>
    Task<FilterSet?> FindFilterSetWithVersionsAsync(
        Guid filterSetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the filter set from the change tracker.
    /// </summary>
    /// <param name="filterSet">The tracked filter set entity to remove.</param>
    void RemoveFilterSet(FilterSet filterSet);

    /// <summary>
    /// Removes the specified filter versions from the change tracker.
    /// </summary>
    /// <param name="versions">The tracked filter versions to remove.</param>
    void RemoveFilterVersions(IReadOnlyList<FilterVersion> versions);

    // -- Rewrite set mutations -------------------------------------------------

    /// <summary>
    /// Registers a new rewrite set with the change tracker.
    /// </summary>
    /// <param name="rewriteSet">The rewrite set entity to begin tracking.</param>
    void AddRewriteSet(RewriteSet rewriteSet);

    /// <summary>
    /// Registers a new rewrite version with the change tracker.
    /// </summary>
    /// <param name="version">The rewrite version entity to begin tracking.</param>
    void AddRewriteVersion(RewriteVersion version);

    /// <summary>
    /// Returns <c>true</c> if any mapping still references the specified rewrite set.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when a mapping still references the rewrite set; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsRewriteSetReferencedAsync(
        Guid rewriteSetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the rewrite set with its versions, or <c>null</c> if it does not exist.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The rewrite set with its versions, or <see langword="null"/> when it does not exist.</returns>
    Task<RewriteSet?> FindRewriteSetWithVersionsAsync(
        Guid rewriteSetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the rewrite set from the change tracker.
    /// </summary>
    /// <param name="rewriteSet">The tracked rewrite set entity to remove.</param>
    void RemoveRewriteSet(RewriteSet rewriteSet);

    /// <summary>
    /// Removes the specified rewrite versions from the change tracker.
    /// </summary>
    /// <param name="versions">The tracked rewrite versions to remove.</param>
    void RemoveRewriteVersions(IReadOnlyList<RewriteVersion> versions);

    // -- Unit of work ---------------------------------------------------------

    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
