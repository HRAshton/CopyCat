namespace CopyCat.Domain.Enums;

/// <summary>
/// Represents the login and connection lifecycle of a Telegram session.
/// </summary>
public enum TelegramSessionStatus
{
    /// <summary>Session created but login has not started.</summary>
    Pending,

    /// <summary>Telegram sent a verification code and is waiting for it.</summary>
    AwaitingCode,

    /// <summary>Telegram accepted the code and is waiting for the 2FA password.</summary>
    AwaitingPassword,

    /// <summary>The session is fully authenticated and connected.</summary>
    Connected,

    /// <summary>The session has been manually disabled.</summary>
    Disabled,

    /// <summary>The last login step raised an error.</summary>
    Faulted,
}
