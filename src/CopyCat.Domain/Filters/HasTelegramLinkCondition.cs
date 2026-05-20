namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks for Telegram links.
/// </summary>
public sealed record HasTelegramLinkCondition(bool Expected = true, string? RuleId = null) : FilterNode(RuleId);
