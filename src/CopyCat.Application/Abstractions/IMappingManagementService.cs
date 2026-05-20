using CopyCat.Application.Models;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Manages channel mappings and per-mapping filter/rewrite rules.
/// </summary>
public interface IMappingManagementService
{
    /// <summary>
    /// Returns a summary of all configured mappings.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of mapping summaries.</returns>
    Task<IReadOnlyList<MappingSummary>> GetMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a mapping and returns the persisted summary.
    /// </summary>
    /// <param name="request">The upsert request describing the mapping.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The persisted mapping summary.</returns>
    Task<MappingSummary> UpsertMappingAsync(
        MappingUpsertRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the inline filter configuration for a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The filter editor model for the mapping.</returns>
    Task<MappingFilterEditorModel> GetMappingFilterEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an updated inline filter for a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="model">The updated filter editor model.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveMappingFilterAsync(
        Guid mappingId,
        MappingFilterEditorModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the inline filter from a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveMappingFilterAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Debugs an ad-hoc filter definition against recent messages of the mapping source.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="definition">The filter definition to evaluate.</param>
    /// <param name="take">Maximum number of messages to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Per-message debug results.</returns>
    Task<IReadOnlyList<FilterDebugResult>> DebugMappingFilterAsync(
        Guid mappingId,
        FilterSetDefinition definition,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the inline rewrite configuration for a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rewrite editor model for the mapping.</returns>
    Task<MappingRewriteEditorModel> GetMappingRewriteEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an updated inline rewrite for a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="model">The updated rewrite editor model.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveMappingRewriteAsync(
        Guid mappingId,
        MappingRewriteEditorModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the inline rewrite from a mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveMappingRewriteAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Debugs ad-hoc rewrite rules against recent messages of the mapping source.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="rules">The rewrite rule set to evaluate.</param>
    /// <param name="take">Maximum number of messages to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Per-message rewrite debug results.</returns>
    Task<IReadOnlyList<RewriteDebugResult>> DebugMappingRewriteAsync(
        Guid mappingId,
        RewriteRuleSet rules,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a one-shot backfill for the specified mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="take">Maximum number of messages to backfill.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunBackfillAsync(Guid mappingId, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a mapping and its rules.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
