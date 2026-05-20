namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents the result of evaluating a filter.
/// </summary>
public sealed record FilterDecision(
    bool Accepted,
    string? MatchedRuleId,
    IReadOnlyList<string> Trace,
    string? Explanation);
