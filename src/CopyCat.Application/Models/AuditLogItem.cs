namespace CopyCat.Application.Models;

/// <summary>
/// Represents an audit log item.
/// </summary>
public sealed record AuditLogItem(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    DateTimeOffset CreatedAt);
