using System.Text.Json;
using System.Text.Json.Serialization;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Reads and writes audit log entries for user-visible administrative actions.
/// </summary>
public sealed class AuditLogService(CopyCatDbContext dbContext) : IAuditLogService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Loads the most recent audit log entries for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>Up to one hundred recent audit log items ordered from newest to oldest.</returns>
    public async Task<IReadOnlyList<AuditLogItem>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLog
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new AuditLogItem(x.Id, x.Action, x.EntityType, x.EntityId, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a new audit log entry describing a state change.
    /// </summary>
    /// <param name="action">The action verb describing what happened.</param>
    /// <param name="entityType">The logical entity type affected by the action.</param>
    /// <param name="entityId">The identifier of the affected entity, when one exists.</param>
    /// <param name="before">The entity snapshot before the change, when available.</param>
    /// <param name="after">The entity snapshot after the change, when available.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the audit entry has been committed.</returns>
    public async Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before,
        object? after,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditLog.Add(
            new AuditLogEntry
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                BeforeJson = before is null ? null : JsonSerializer.Serialize(before, AuditJsonOptions),
                AfterJson = after is null ? null : JsonSerializer.Serialize(after, AuditJsonOptions),
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
