using CopyCat.Application.Models;
using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides data access for forwarding worker batches.
/// </summary>
public interface IForwardingWorkStore
{
    /// <summary>
    /// Loads jobs ready for execution.
    /// </summary>
    /// <param name="take">The maximum number of ready jobs to return.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The next forwarding jobs ready to execute.</returns>
    Task<IReadOnlyList<ForwardingJob>> GetReadyJobsAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all entities required to execute a forwarding job.
    /// </summary>
    /// <param name="jobId">The identifier of the forwarding job to hydrate.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The forwarding execution context for the requested job.</returns>
    Task<ForwardingExecutionContext> GetExecutionContextAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
