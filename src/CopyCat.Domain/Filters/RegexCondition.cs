using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a regex-based filter condition.
/// </summary>
public sealed record RegexCondition(
    string Pattern,
    TextField Field,
    string? RuleId = null) : FilterNode(RuleId);
