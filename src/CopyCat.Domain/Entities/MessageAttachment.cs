using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a stored message attachment.
/// </summary>
public sealed class MessageAttachment
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the owning message identifier.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the owning message.
    /// </summary>
    public StoredMessage Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the attachment type.
    /// </summary>
    public AttachmentType AttachmentType { get; set; }

    /// <summary>
    /// Gets or sets the Telegram file identifier.
    /// </summary>
    public string? TelegramFileId { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific unique file identifier.
    /// </summary>
    public string? FileUniqueId { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the raw metadata JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
