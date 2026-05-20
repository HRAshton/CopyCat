using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a logical group of child filter nodes.
/// </summary>
public sealed record ConditionGroup(
    LogicalOperator Operator,
    IReadOnlyList<FilterNode> Children,
    string? GroupRuleId = null) : FilterNode(GroupRuleId);
