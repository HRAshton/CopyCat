namespace CopyCat.Application.Abstractions;

/// <summary>
/// Provides in-memory per-session Telegram authentication traces.
/// </summary>
public interface ITelegramAuthTraceStore
{
    /// <summary>
    /// Clears the trace for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    void Clear(Guid sessionId);

    /// <summary>
    /// Appends a trace message for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="message">The trace message to record.</param>
    void Record(Guid sessionId, string message);

    /// <summary>
    /// Gets the latest trace text for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The latest trace string, or <c>null</c> if none has been recorded.</returns>
    string? GetLatest(Guid sessionId);
}
