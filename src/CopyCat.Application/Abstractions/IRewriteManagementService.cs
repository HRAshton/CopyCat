using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Manages rewrite sets and their published versions.
/// </summary>
public interface IRewriteManagementService
{
    /// <summary>
    /// Returns a summary of every saved rewrite set.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of rewrite set summaries.</returns>
    Task<IReadOnlyList<RewriteSetSummary>> GetRewriteSetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new draft version for the specified (or new) rewrite set.
    /// </summary>
    /// <param name="model">The editor model containing the rewrite configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The summary of the saved rewrite set.</returns>
    Task<RewriteSetSummary> SaveDraftAsync(RewriteSetEditorModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes the latest draft version of the rewrite set to Published status.
    /// </summary>
    /// <param name="rewriteSetId">The rewrite set identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishLatestAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes a rewrite set that is not referenced by any mapping.
    /// </summary>
    /// <param name="rewriteSetId">The rewrite set identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteRewriteSetAsync(Guid rewriteSetId, CancellationToken cancellationToken = default);
}
