namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks whether text is present.
/// </summary>
public sealed record HasTextCondition(bool Expected, string? RuleId = null) : FilterNode(RuleId);
