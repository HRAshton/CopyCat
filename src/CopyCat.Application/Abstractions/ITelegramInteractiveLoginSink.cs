namespace CopyCat.Application.Abstractions;

/// <summary>
/// Persists the outcome of long-running interactive Telegram login operations.
/// </summary>
public interface ITelegramInteractiveLoginSink
{
    /// <summary>
    /// Marks a session as connected and stores the latest Telegram session payload.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="sessionDataEncrypted">The encrypted Telegram session bytes.</param>
    /// <param name="connectedAt">The timestamp when the connection was established.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteLoginAsync(
        Guid sessionId,
        string sessionDataEncrypted,
        DateTimeOffset connectedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a session as faulted with the provided error.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailLoginAsync(Guid sessionId, string errorMessage, CancellationToken cancellationToken = default);
}
