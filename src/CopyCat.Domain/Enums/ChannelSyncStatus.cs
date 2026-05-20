namespace CopyCat.Domain.Enums;

/// <summary>
/// Tracks the live-ingest synchronisation state of a channel.
/// </summary>
public enum ChannelSyncStatus
{
    /// <summary>No synchronisation has run yet.</summary>
    Idle,

    /// <summary>The channel is being polled and is up to date.</summary>
    Live,

    /// <summary>A backfill pass completed successfully.</summary>
    Backfilled,

    /// <summary>The last synchronisation attempt failed.</summary>
    Failed,
}
