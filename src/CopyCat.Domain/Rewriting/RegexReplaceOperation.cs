namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Replaces text using a regular expression.
/// </summary>
public sealed record RegexReplaceOperation(string Pattern, string Replacement) : RewriteOperation;
