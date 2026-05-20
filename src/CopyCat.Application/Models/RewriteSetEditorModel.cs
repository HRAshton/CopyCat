using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a rewrite set editor model.
/// </summary>
public sealed record RewriteSetEditorModel(
    Guid? RewriteSetId,
    string Name,
    string? Description,
    RewriteRuleSet Rules);
