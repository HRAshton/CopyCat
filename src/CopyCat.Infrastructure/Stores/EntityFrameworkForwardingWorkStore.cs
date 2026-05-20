using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkForwardingWorkStore(CopyCatDbContext dbContext) : IForwardingWorkStore
{
    public async Task<IReadOnlyList<ForwardingJob>> GetReadyJobsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ForwardingJobs
            .Where(x =>
                (x.Status == ForwardingJobStatus.Pending || x.Status == ForwardingJobStatus.FailedTransient)
                && (!x.NextRetryAt.HasValue || x.NextRetryAt <= DateTimeOffset.UtcNow))
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ForwardingExecutionContext> GetExecutionContextAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ForwardingJob job = await dbContext.ForwardingJobs.FirstAsync(x => x.Id == jobId, cancellationToken);
        ChannelMapping mapping =
            await dbContext.ChannelMappings.FirstAsync(x => x.Id == job.MappingId, cancellationToken);
        TelegramChannel source = await dbContext.TelegramChannels.FirstAsync(
            x => x.Id == mapping.SourceChannelId,
            cancellationToken);
        TelegramChannel target = await dbContext.TelegramChannels.FirstAsync(
            x => x.Id == mapping.TargetChannelId,
            cancellationToken);
        TelegramSession session = await dbContext.TelegramSessions.FirstAsync(
            x => x.Id == source.SessionId,
            cancellationToken);
        StoredMessage message = await dbContext.Messages
            .Include(x => x.Attachments)
            .Include(x => x.Links)
            .FirstAsync(x => x.Id == job.MessageId, cancellationToken);
        RewriteVersion? rewriteVersion = job.RewriteVersionId.HasValue
            ? await dbContext.RewriteVersions.FirstOrDefaultAsync(
                x => x.Id == job.RewriteVersionId.Value,
                cancellationToken)
            : null;

        return new ForwardingExecutionContext(job, mapping, source, target, session, message, rewriteVersion);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
