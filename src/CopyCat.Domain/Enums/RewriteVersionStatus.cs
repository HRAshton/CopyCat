namespace CopyCat.Domain.Enums;

/// <summary>
/// Represents the publication lifecycle of a rewrite version.
/// </summary>
public enum RewriteVersionStatus
{
    /// <summary>Version is being edited and has not been published.</summary>
    Draft,

    /// <summary>Version is currently active.</summary>
    Published,

    /// <summary>Version has been superseded by a newer published version.</summary>
    Archived,
}
