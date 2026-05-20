using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a Telegram channel or chat discovered for a session.
/// </summary>
public sealed class TelegramChannel : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the owning session identifier.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the owning session.
    /// </summary>
    public TelegramSession Session { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Telegram channel identifier.
    /// </summary>
    public long TelegramChannelId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram access hash.
    /// </summary>
    public string? AccessHash { get; set; }

    /// <summary>
    /// Gets or sets the channel title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public TelegramChannelType ChannelType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the channel is a source.
    /// </summary>
    public bool IsSource { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the channel is a target.
    /// </summary>
    public bool IsTarget { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account can post.
    /// </summary>
    public bool? CanPost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account has admin rights.
    /// </summary>
    public bool? CanAdmin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether related targets can be created.
    /// </summary>
    public bool? CanCreateRelatedTargets { get; set; }

    /// <summary>
    /// Gets or sets the discovery time.
    /// </summary>
    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the raw serialized Telegram payload.
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
