using CopyCat.Domain.Enums;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a version of a rewrite set.
/// </summary>
public sealed class RewriteVersion
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the parent rewrite set identifier.
    /// </summary>
    public Guid RewriteSetId { get; set; }

    /// <summary>
    /// Gets or sets the parent rewrite set.
    /// </summary>
    public RewriteSet RewriteSet { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Gets or sets the version status.
    /// </summary>
    public RewriteVersionStatus Status { get; set; } = RewriteVersionStatus.Draft;

    /// <summary>
    /// Gets or sets the rewrite rules.
    /// </summary>
    public RewriteRuleSet Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the creator identifier.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the publication time.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }
}
