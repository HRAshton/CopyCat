namespace CopyCat.Domain.Enums;

/// <summary>
/// Represents the processing lifecycle of a Telegram control operation.
/// </summary>
public enum TelegramControlOperationStatus
{
    /// <summary>Operation is queued and waiting to be picked up.</summary>
    Pending,

    /// <summary>Operation is currently being executed.</summary>
    Processing,

    /// <summary>Operation completed successfully.</summary>
    Succeeded,

    /// <summary>Operation failed.</summary>
    Failed,
}
