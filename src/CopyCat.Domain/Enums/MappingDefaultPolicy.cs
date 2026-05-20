namespace CopyCat.Domain.Enums;

/// <summary>
/// Determines what happens to messages that do not match any filter rule.
/// </summary>
public enum MappingDefaultPolicy
{
    /// <summary>Pass through unmatched messages.</summary>
    Allow,

    /// <summary>Drop unmatched messages.</summary>
    Reject,
}
