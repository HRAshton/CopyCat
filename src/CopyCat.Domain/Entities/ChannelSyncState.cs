using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Tracks synchronization progress for a channel.
/// </summary>
public sealed class ChannelSyncState : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the last seen live message identifier.
    /// </summary>
    public long? LastSeenMessageId { get; set; }

    /// <summary>
    /// Gets or sets the last backfilled message identifier.
    /// </summary>
    public long? LastBackfilledMessageId { get; set; }

    /// <summary>
    /// Gets or sets the last synchronization time.
    /// </summary>
    public DateTimeOffset? LastSyncAt { get; set; }

    /// <summary>
    /// Gets or sets the synchronization status.
    /// </summary>
    public ChannelSyncStatus SyncStatus { get; set; } = ChannelSyncStatus.Idle;

    /// <summary>
    /// Gets or sets the last synchronization error.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
