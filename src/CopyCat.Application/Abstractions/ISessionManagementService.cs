using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Manages Telegram session lifecycle, authentication, and connection.
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Returns a summary of all configured sessions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of session summaries.</returns>
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new session and returns a summary with the assigned identifier.
    /// </summary>
    /// <param name="request">The session creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The summary of the newly created session.</returns>
    Task<SessionSummary> CreateSessionAsync(
        SessionCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the interactive phone-number login flow for the session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartLoginAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the QR-code login flow for the session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartQrLoginAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a Telegram verification code received during login.
    /// </summary>
    /// <param name="request">The login code submission request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitCodeAsync(LoginCodeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-sends the Telegram verification code for the in-progress login.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResendCodeAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits the two-factor authentication password for the session.
    /// </summary>
    /// <param name="request">The login password submission request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitPasswordAsync(LoginPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables an active session without deleting it.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisableSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the login flow for a disabled or faulted session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReconnectSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes the session and all dependent data.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
