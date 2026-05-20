using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Models;

/// <summary>
/// A read-only summary of a rewrite set returned to callers outside the Application layer.
/// </summary>
public sealed record RewriteSetSummary(
    Guid Id,
    string Name,
    string? Description,
    int VersionCount,
    RewriteRuleSet? LatestRules);
