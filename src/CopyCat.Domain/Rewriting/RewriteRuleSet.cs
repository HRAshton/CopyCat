namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Represents a set of rewrite operations.
/// </summary>
public sealed record RewriteRuleSet(IReadOnlyList<RewriteOperation>? Operations = null)
{
    /// <summary>
    /// Gets the effective operations.
    /// </summary>
    public IReadOnlyList<RewriteOperation> EffectiveOperations => Operations ?? [];
}
