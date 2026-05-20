namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Represents the output of rewriting a message.
/// </summary>
public sealed record RewriteResult(
    string? Text,
    string? Caption,
    bool DropMedia,
    IReadOnlyList<string> Trace);
