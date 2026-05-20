namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a stored source message.
/// </summary>
public sealed class StoredMessage : IHasAuditTimestamps
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
    /// Gets or sets the source channel identifier.
    /// </summary>
    public Guid SourceChannelId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram message identifier.
    /// </summary>
    public long TelegramMessageId { get; set; }

    /// <summary>
    /// Gets or sets the grouped media identifier.
    /// </summary>
    public string? GroupedId { get; set; }

    /// <summary>
    /// Gets or sets the original message time.
    /// </summary>
    public DateTimeOffset MessageDate { get; set; }

    /// <summary>
    /// Gets or sets the edit time.
    /// </summary>
    public DateTimeOffset? EditDate { get; set; }

    /// <summary>
    /// Gets or sets the message text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the media caption.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Gets or sets the normalized text.
    /// </summary>
    public string? NormalizedText { get; set; }

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

    /// <summary>
    /// Gets or sets the attachments.
    /// </summary>
    public List<MessageAttachment> Attachments { get; set; } = [];

    /// <summary>
    /// Gets or sets the extracted links.
    /// </summary>
    public List<MessageLink> Links { get; set; } = [];
}
