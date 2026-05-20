namespace CopyCat.Domain.Enums;

/// <summary>
/// Identifies the type of media attached to a message.
/// </summary>
public enum AttachmentType
{
    /// <summary>A photo.</summary>
    Photo,

    /// <summary>A video.</summary>
    Video,

    /// <summary>A generic document/file.</summary>
    Document,

    /// <summary>An audio track.</summary>
    Audio,

    /// <summary>A voice message.</summary>
    Voice,

    /// <summary>A sticker.</summary>
    Sticker,

    /// <summary>An animated GIF or animation.</summary>
    Animation,

    /// <summary>Any other media type.</summary>
    Other,
}
