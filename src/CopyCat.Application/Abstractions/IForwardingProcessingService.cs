namespace CopyCat.Application.Abstractions;

/// <summary>
/// Processes pending forwarding jobs.
/// </summary>
public interface IForwardingProcessingService
{
    /// <summary>
    /// Executes a single forwarding batch.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessBatchAsync(CancellationToken cancellationToken = default);
}
