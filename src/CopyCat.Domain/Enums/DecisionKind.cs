namespace CopyCat.Domain.Enums;

/// <summary>
/// Records the outcome of evaluating a message against a mapping's filter.
/// </summary>
public enum DecisionKind
{
    /// <summary>Message passed the filter and will be forwarded.</summary>
    Accepted,

    /// <summary>Message was blocked by the filter.</summary>
    Rejected,

    /// <summary>Message requires manual review.</summary>
    Review,
}
