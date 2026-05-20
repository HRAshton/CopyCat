namespace CopyCat.Application.Models;

/// <summary>
/// Represents the result of debugging a filter.
/// </summary>
public sealed record FilterDebugResult(
    long TelegramMessageId,
    DateTimeOffset MessageDate,
    string Preview,
    bool Accepted,
    string? MatchedRuleId,
    IReadOnlyList<string> Trace,
    string? RewritePreview);
