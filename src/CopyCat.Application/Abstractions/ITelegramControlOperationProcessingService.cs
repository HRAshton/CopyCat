namespace CopyCat.Application.Abstractions;

/// <summary>
/// Processes queued Telegram control operations.
/// </summary>
public interface ITelegramControlOperationProcessingService
{
    /// <summary>
    /// Executes at most one queued control operation.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> when an operation was processed; otherwise <c>false</c>.</returns>
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}
