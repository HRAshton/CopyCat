using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks for words in a message.
/// </summary>
public sealed record ContainsWordsCondition(
    IReadOnlyList<string> Words,
    MatchMode MatchMode,
    bool CaseSensitive,
    bool IsWhitelist,
    string? RuleId = null) : FilterNode(RuleId);
