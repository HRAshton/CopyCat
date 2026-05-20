namespace CopyCat.Application.Abstractions;

/// <summary>
/// Processes live-ingest polling batches.
/// </summary>
public interface ILiveIngestProcessingService
{
    /// <summary>
    /// Executes a single live-ingest batch.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessBatchAsync(CancellationToken cancellationToken = default);
}
