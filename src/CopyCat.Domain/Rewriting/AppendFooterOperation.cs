namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Appends footer text.
/// </summary>
public sealed record AppendFooterOperation(string Text) : RewriteOperation;
