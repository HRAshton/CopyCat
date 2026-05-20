namespace CopyCat.Application.Models;

/// <summary>
/// Represents the result of debugging rewrites against a stored message.
/// </summary>
public sealed record RewriteDebugResult(
    long TelegramMessageId,
    DateTimeOffset MessageDate,
    string Preview,
    string? RewrittenText,
    string? RewrittenCaption,
    bool DropMedia,
    IReadOnlyList<string> Trace);
