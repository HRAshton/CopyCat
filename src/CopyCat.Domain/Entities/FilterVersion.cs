using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a version of a filter set.
/// </summary>
public sealed class FilterVersion
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the parent filter set identifier.
    /// </summary>
    public Guid FilterSetId { get; set; }

    /// <summary>
    /// Gets or sets the parent filter set.
    /// </summary>
    public FilterSet FilterSet { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Gets or sets the version status.
    /// </summary>
    public FilterVersionStatus Status { get; set; } = FilterVersionStatus.Draft;

    /// <summary>
    /// Gets or sets the filter definition.
    /// </summary>
    public FilterSetDefinition FilterDefinition { get; set; } = FilterSetDefinition.AllowAll();

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
