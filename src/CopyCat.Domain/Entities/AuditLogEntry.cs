namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents an audit log entry.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the actor user identifier.
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the serialized state before the action.
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    /// Gets or sets the serialized state after the action.
    /// </summary>
    public string? AfterJson { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
