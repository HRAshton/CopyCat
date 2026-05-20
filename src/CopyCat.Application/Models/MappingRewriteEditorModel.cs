using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents the inline rewrite configuration for a mapping.
/// </summary>
public sealed record MappingRewriteEditorModel(
    bool IsEnabled,
    RewriteRuleSet Rules);
