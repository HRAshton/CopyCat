namespace CopyCat.Domain.Enums;

/// <summary>
/// Controls how a message is forwarded to the target channel.
/// </summary>
public enum ForwardingMode
{
    /// <summary>Use the Telegram native forward (shows original author).</summary>
    NativeForward,

    /// <summary>Re-post as a copy without modification.</summary>
    CopyAsIs,

    /// <summary>Re-post after applying the configured rewrite rules.</summary>
    CopyWithRewriting,

    /// <summary>Forward attachments only; strip all text.</summary>
    AttachmentsWithoutText,

    /// <summary>Forward text only; drop media.</summary>
    TextOnly,
}
