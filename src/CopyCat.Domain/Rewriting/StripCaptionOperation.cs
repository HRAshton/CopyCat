namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Removes the caption from a media message.
/// </summary>
public sealed record StripCaptionOperation() : RewriteOperation;
