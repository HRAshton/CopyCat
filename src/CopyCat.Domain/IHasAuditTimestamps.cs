namespace CopyCat.Domain;

/// <summary>
/// Marks an entity that carries creation and last-updated timestamps.
/// </summary>
public interface IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the creation time (UTC).
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the time of the last update (UTC).
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }
}
