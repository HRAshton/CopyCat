namespace CopyCat.Domain.Enums;

/// <summary>
/// Identifies the type of an asynchronous Telegram control operation.
/// </summary>
public enum TelegramControlOperationType
{
    /// <summary>Discover channels accessible by the session.</summary>
    DiscoverChannels,

    /// <summary>Create a new target channel for a source.</summary>
    CreateTargetChannel,

    /// <summary>Backfill historical messages for a mapping.</summary>
    RunBackfill,
}
