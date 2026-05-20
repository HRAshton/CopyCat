namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks for any URL.
/// </summary>
public sealed record HasAnyUrlCondition(bool Expected = true, string? RuleId = null) : FilterNode(RuleId);
