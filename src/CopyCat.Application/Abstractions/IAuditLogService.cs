using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Writes and retrieves structured audit log entries.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Returns the most recent audit log entries.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of recent audit log items.</returns>
    Task<IReadOnlyList<AuditLogItem>> GetRecentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a structured audit log entry.
    /// </summary>
    /// <param name="action">The action that was performed (e.g. "Created", "Updated").</param>
    /// <param name="entityType">The type of entity the action was performed on.</param>
    /// <param name="entityId">The identifier of the affected entity, if applicable.</param>
    /// <param name="before">The entity state before the action, if applicable.</param>
    /// <param name="after">The entity state after the action, if applicable.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before,
        object? after,
        CancellationToken cancellationToken = default);
}
