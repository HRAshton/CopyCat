using CopyCat.Application.Abstractions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Exposes forwarding job history and administrative retry actions.
/// </summary>
public sealed class ForwardingJobService(CopyCatDbContext dbContext) : IForwardingJobService
{
    /// <summary>
    /// Loads recent forwarding jobs with their source and target channel names.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>Up to two hundred recent forwarding jobs ordered from newest to oldest.</returns>
    public async Task<IReadOnlyList<ForwardingJobItem>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ForwardingJobs
            .Join(
                dbContext.ChannelMappings,
                job => job.MappingId,
                mapping => mapping.Id,
                (job, mapping) => new { job, mapping })
            .Join(
                dbContext.TelegramChannels,
                pair => pair.mapping.SourceChannelId,
                source => source.Id,
                (pair, source) => new { pair.job, pair.mapping, source })
            .Join(
                dbContext.TelegramChannels,
                pair => pair.mapping.TargetChannelId,
                target => target.Id,
                (pair, target) => new { pair.job, pair.source, target })
            .OrderByDescending(x => x.job.CreatedAt)
            .Take(200)
            .Select(x => new ForwardingJobItem(
                x.job.Id,
                x.source.Title,
                x.target.Title,
                x.job.Status,
                x.job.ForwardingMode,
                x.job.Attempts,
                x.job.LastError,
                x.job.CreatedAt,
                x.job.NextRetryAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a failed or stalled forwarding job to the pending state so workers can retry it.
    /// </summary>
    /// <param name="jobId">The identifier of the forwarding job to retry.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the job has been marked pending again.</returns>
    public async Task RetryJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        ForwardingJob job = await dbContext.ForwardingJobs.FirstAsync(x => x.Id == jobId, cancellationToken);
        job.Status = ForwardingJobStatus.Pending;
        job.NextRetryAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
