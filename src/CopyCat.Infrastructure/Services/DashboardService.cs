using CopyCat.Application.Abstractions;
using CopyCat.Application.Models;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Aggregates high-level operational metrics for the dashboard.
/// </summary>
public sealed class DashboardService(CopyCatDbContext dbContext) : IDashboardService
{
    /// <summary>
    /// Builds the current dashboard snapshot from persisted system state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The current dashboard counts and recent session errors.</returns>
    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        int enabledSessions = await dbContext.TelegramSessions.CountAsync(x => x.IsEnabled, cancellationToken);
        int unhealthySessions = await dbContext.TelegramSessions.CountAsync(
            x => x.Status == TelegramSessionStatus.Faulted,
            cancellationToken);
        int activeSourceChannels = await dbContext.TelegramChannels.CountAsync(x => x.IsSource, cancellationToken);
        int activeMappings = await dbContext.ChannelMappings.CountAsync(x => x.IsEnabled, cancellationToken);
        int pendingJobs = await dbContext.ForwardingJobs.CountAsync(
            x => x.Status == ForwardingJobStatus.Pending || x.Status == ForwardingJobStatus.Processing,
            cancellationToken);
        int failedJobs = await dbContext.ForwardingJobs.CountAsync(
            x => x.Status == ForwardingJobStatus.FailedPermanent || x.Status == ForwardingJobStatus.FailedTransient,
            cancellationToken);
        List<string> latestErrors = await dbContext.TelegramSessions.Where(x => x.LastError != null)
            .OrderByDescending(x => x.UpdatedAt).Select(x => $"{x.Name}: {x.LastError}").Take(5)
            .ToListAsync(cancellationToken);
        return new DashboardSnapshot(
            enabledSessions,
            unhealthySessions,
            activeSourceChannels,
            activeMappings,
            pendingJobs,
            failedJobs,
            latestErrors);
    }
}
