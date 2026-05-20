namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks whether a message was edited.
/// </summary>
public sealed record IsEditedCondition(bool Expected, string? RuleId = null) : FilterNode(RuleId);
