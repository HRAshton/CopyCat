namespace CopyCat.Domain.Enums;

/// <summary>
/// Controls how many words must match in a word-list condition.
/// </summary>
public enum MatchMode
{
    /// <summary>At least one word must be present.</summary>
    Any,

    /// <summary>All words must be present.</summary>
    All,
}
