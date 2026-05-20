namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Replaces exact text occurrences.
/// </summary>
public sealed record ReplaceExactTextOperation(string Search, string Replacement) : RewriteOperation;
