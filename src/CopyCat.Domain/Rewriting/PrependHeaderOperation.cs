namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Prepends header text.
/// </summary>
public sealed record PrependHeaderOperation(string Text) : RewriteOperation;
