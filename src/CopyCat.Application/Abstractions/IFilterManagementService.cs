using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Manages filter sets and their published versions.
/// </summary>
public interface IFilterManagementService
{
    /// <summary>
    /// Returns a summary of every saved filter set.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of filter set summaries.</returns>
    Task<IReadOnlyList<FilterSetSummary>> GetFilterSetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new draft version for the specified (or new) filter set.
    /// </summary>
    /// <param name="model">The editor model containing the filter configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The summary of the saved filter set.</returns>
    Task<FilterSetSummary> SaveDraftAsync(FilterSetEditorModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes the latest draft version of the filter set to Published status.
    /// </summary>
    /// <param name="filterSetId">The filter set identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishLatestAsync(Guid filterSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the latest published version of the filter set against recent messages
    /// and returns per-message debug results.
    /// </summary>
    /// <param name="filterSetId">The filter set identifier.</param>
    /// <param name="channelId">If provided, limits source messages to the specified channel.</param>
    /// <param name="take">Maximum number of messages to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Per-message filter debug results.</returns>
    Task<IReadOnlyList<FilterDebugResult>> DebugAsync(
        Guid filterSetId,
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes a filter set that is not referenced by any mapping.
    /// </summary>
    /// <param name="filterSetId">The filter set identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteFilterSetAsync(Guid filterSetId, CancellationToken cancellationToken = default);
}
