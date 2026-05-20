using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Queries and manages forwarding job records.
/// </summary>
public interface IForwardingJobService
{
    /// <summary>
    /// Returns a list of recent forwarding jobs.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of forwarding job items.</returns>
    Task<IReadOnlyList<ForwardingJobItem>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a failed job so it will be retried.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RetryJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
