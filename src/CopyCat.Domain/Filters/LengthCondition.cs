namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks message length.
/// </summary>
public sealed record LengthCondition(
    int? Minimum,
    int? Maximum,
    string? RuleId = null) : FilterNode(RuleId);
