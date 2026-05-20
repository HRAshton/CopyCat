namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents an extracted link from a stored message.
/// </summary>
public sealed class MessageLink
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
    /// Gets or sets the URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the link type.
    /// </summary>
    public string LinkType { get; set; } = "Url";

    /// <summary>
    /// Gets or sets the display text.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Gets or sets the raw metadata JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
