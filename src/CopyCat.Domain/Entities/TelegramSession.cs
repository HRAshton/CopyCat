using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a Telegram session used by the application.
/// </summary>
public sealed class TelegramSession : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the masked phone number.
    /// </summary>
    public string? PhoneNumberMasked { get; set; }

    /// <summary>
    /// Gets or sets the encrypted phone number.
    /// </summary>
    public string? PhoneNumberEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the encrypted API identifier.
    /// </summary>
    public string? ApiIdEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the encrypted API hash.
    /// </summary>
    public string? ApiHashEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the encrypted Telegram session payload.
    /// </summary>
    public string SessionDataEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current session status.
    /// </summary>
    public TelegramSessionStatus Status { get; set; } = TelegramSessionStatus.Pending;

    /// <summary>
    /// Gets or sets the last successful connection time.
    /// </summary>
    public DateTimeOffset? LastConnectedAt { get; set; }

    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the pending login challenge marker.
    /// </summary>
    public string? PendingChallenge { get; set; }

    /// <summary>
    /// Gets or sets the pending phone number for an interactive login flow.
    /// </summary>
    public string? PendingPhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the discovered channels for the session.
    /// </summary>
    public List<TelegramChannel> Channels { get; set; } = [];

    // -- Behaviour ------------------------------------------------------------

    /// <summary>
    /// Transitions the session to the <see cref="TelegramSessionStatus.Connected"/> state after a
    /// successful interactive login completion.
    /// </summary>
    /// <param name="sessionDataEncrypted">The newly obtained encrypted session blob.</param>
    /// <param name="connectedAt">The timestamp at which the connection was confirmed.</param>
    public void MarkConnected(string sessionDataEncrypted, DateTimeOffset connectedAt)
    {
        SessionDataEncrypted = sessionDataEncrypted;
        IsEnabled = true;
        Status = TelegramSessionStatus.Connected;
        PendingChallenge = null;
        LastConnectedAt = connectedAt;
        LastError = null;
    }

    /// <summary>
    /// Transitions the session to the <see cref="TelegramSessionStatus.Faulted"/> state and
    /// records the error message.
    /// </summary>
    /// <param name="error">The human-readable error description.</param>
    public void MarkFaulted(string error)
    {
        Status = TelegramSessionStatus.Faulted;
        LastError = error;
    }

    /// <summary>
    /// Clears all transient authentication state so that a fresh Telegram login can begin.
    /// </summary>
    /// <param name="encryptedEmptySession">A freshly encrypted empty session payload.</param>
    public void ResetForFreshLogin(string encryptedEmptySession)
    {
        Status = TelegramSessionStatus.Pending;
        PendingChallenge = null;
        LastError = null;
        SessionDataEncrypted = encryptedEmptySession;
    }

    /// <summary>
    /// Applies a login-progress update received from the Telegram client, advancing the session
    /// status and recording the current challenge step.
    /// </summary>
    /// <param name="nextChallenge">
    /// The next challenge string returned by the Telegram API, or <c>null</c> if login is complete.
    /// </param>
    /// <param name="status">The resolved <see cref="TelegramSessionStatus"/> for this step.</param>
    /// <param name="connectedAt">
    /// The moment the session connected, populated only when <paramref name="status"/> is
    /// <see cref="TelegramSessionStatus.Connected"/>.
    /// </param>
    public void ApplyLoginProgress(string? nextChallenge, TelegramSessionStatus status, DateTimeOffset? connectedAt)
    {
        Status = status;
        PendingChallenge = nextChallenge;
        LastError = null;

        if (status == TelegramSessionStatus.Connected)
        {
            LastConnectedAt = connectedAt ?? DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Disables the session and marks it as <see cref="TelegramSessionStatus.Disabled"/>.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        Status = TelegramSessionStatus.Disabled;
    }

    /// <summary>
    /// Re-enables the session so that it can be reconnected.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Clears the last recorded error without changing the session status.
    /// </summary>
    public void ClearError()
    {
        LastError = null;
    }
}
