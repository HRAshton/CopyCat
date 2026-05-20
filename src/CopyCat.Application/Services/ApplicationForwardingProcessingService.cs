using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Models;
using CopyCat.Application.Options;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Messages;
using CopyCat.Domain.Rewriting;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates forwarding worker batches.
/// </summary>
internal sealed class ApplicationForwardingProcessingService(
    IForwardingWorkStore forwardingWorkStore,
    ITelegramGateway telegramGateway,
    IMessageRewriter messageRewriter,
    IOptions<ApplicationWorkerOptions> options,
    ILogger<ApplicationForwardingProcessingService> logger) : IForwardingProcessingService
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Forwarding batches must persist per-job failure state and continue processing the batch.")]
    public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ForwardingJob> jobs = await forwardingWorkStore.GetReadyJobsAsync(
            options.Value.ForwardingBatchSize,
            cancellationToken);
        foreach (ForwardingJob job in jobs)
        {
            try
            {
                job.MarkProcessing();
                await forwardingWorkStore.SaveChangesAsync(cancellationToken);

                ForwardingExecutionContext context =
                    await forwardingWorkStore.GetExecutionContextAsync(job.Id, cancellationToken);
                RewriteResult? rewrite = context.RewriteVersion is null
                    ? null
                    : messageRewriter.Rewrite(
                        NormalizedTelegramMessage.FromEntity(context.Message),
                        context.RewriteVersion.Rules);

                long? targetMessageId = await telegramGateway.ExecuteForwardingAsync(
                    context.Session,
                    context.SourceChannel,
                    context.TargetChannel,
                    context.Message,
                    job.ForwardingMode,
                    rewrite,
                    cancellationToken);
                job.MarkSucceeded(targetMessageId);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Forwarding job {JobId} failed (attempt {Attempt}).",
                    job.Id,
                    job.Attempts + 1);
                job.RecordAttemptFailure(exception.Message, maxAttempts: 5);
            }

            await forwardingWorkStore.SaveChangesAsync(cancellationToken);
        }
    }
}
