using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Provides a snapshot of the current application operational state.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns a point-in-time snapshot of key counters and statuses.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The dashboard snapshot.</returns>
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
