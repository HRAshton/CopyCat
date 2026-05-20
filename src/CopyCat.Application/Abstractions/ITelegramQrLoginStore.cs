namespace CopyCat.Application.Abstractions;

/// <summary>
/// Stores in-memory QR login state for Telegram sessions.
/// </summary>
public interface ITelegramQrLoginStore
{
    /// <summary>
    /// Gets the current QR login URL for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The QR code URL, or <c>null</c> if not set.</returns>
    string? GetUrl(Guid sessionId);

    /// <summary>
    /// Clears the QR login state for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    void Clear(Guid sessionId);

    /// <summary>
    /// Updates the QR login URL for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="url">The new QR code URL to store.</param>
    void SetUrl(Guid sessionId, string url);
}
