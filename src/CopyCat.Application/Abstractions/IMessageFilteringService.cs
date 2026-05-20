namespace CopyCat.Application.Abstractions;

/// <summary>
/// Processes pending stored messages through enabled mappings.
/// </summary>
public interface IMessageFilteringService
{
    /// <summary>
    /// Executes a single filtering batch.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessBatchAsync(CancellationToken cancellationToken = default);
}
