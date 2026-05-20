using CopyCat.Application.Models;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides inline mapping rule editing and debugging operations.
/// </summary>
public interface IMappingRuleEditor
{
    /// <summary>
    /// Loads the inline filter editor model for a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping being edited.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The filter editor model for the mapping.</returns>
    Task<MappingFilterEditorModel> GetFilterEditorAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the inline filter configuration for a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping being edited.</param>
    /// <param name="model">The filter editor model to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the filter configuration has been saved.</returns>
    Task SaveFilterAsync(
        Guid mappingId,
        MappingFilterEditorModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the inline filter from a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping whose inline filter should be removed.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the inline filter has been removed.</returns>
    Task RemoveFilterAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Debugs a filter definition against recent messages of the mapping source.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping whose source messages should be sampled.</param>
    /// <param name="definition">The filter definition to evaluate.</param>
    /// <param name="take">The maximum number of recent messages to test.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The filter debug results for each sampled message.</returns>
    Task<IReadOnlyList<FilterDebugResult>> DebugFilterAsync(
        Guid mappingId,
        FilterSetDefinition definition,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the inline rewrite editor model for a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping being edited.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The rewrite editor model for the mapping.</returns>
    Task<MappingRewriteEditorModel> GetRewriteEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the inline rewrite configuration for a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping being edited.</param>
    /// <param name="model">The rewrite editor model to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the rewrite configuration has been saved.</returns>
    Task SaveRewriteAsync(
        Guid mappingId,
        MappingRewriteEditorModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the inline rewrite from a mapping.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping whose inline rewrite should be removed.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the inline rewrite has been removed.</returns>
    Task RemoveRewriteAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Debugs rewrite rules against recent messages of the mapping source.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping whose source messages should be sampled.</param>
    /// <param name="rules">The rewrite rules to apply.</param>
    /// <param name="take">The maximum number of recent messages to test.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The rewrite debug results for each sampled message.</returns>
    Task<IReadOnlyList<RewriteDebugResult>> DebugRewriteAsync(
        Guid mappingId,
        RewriteRuleSet rules,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a filter set when no mappings reference it any longer.
    /// </summary>
    /// <param name="filterSetId">The identifier of the filter set to delete if orphaned.</param>
    /// <param name="cancellationToken">The cancellation token for the delete operation.</param>
    /// <returns>A task that completes when the orphaned filter set cleanup has finished.</returns>
    Task DeleteFilterSetIfOrphanedAsync(Guid filterSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rewrite set when no mappings reference it any longer.
    /// </summary>
    /// <param name="rewriteSetId">The identifier of the rewrite set to delete if orphaned.</param>
    /// <param name="cancellationToken">The cancellation token for the delete operation.</param>
    /// <returns>A task that completes when the orphaned rewrite set cleanup has finished.</returns>
    Task DeleteRewriteSetIfOrphanedAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);
}
