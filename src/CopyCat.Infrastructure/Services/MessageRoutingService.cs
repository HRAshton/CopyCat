using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Messages;
using CopyCat.Domain.Rewriting;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Npgsql;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Routes stored messages through mapping filters and rewrites and enqueues forwarding jobs.
/// </summary>
public sealed class MessageRoutingService(
    CopyCatDbContext dbContext,
    IFilterEvaluator filterEvaluator,
    IMessageRewriter messageRewriter) : IMessageRoutingService
{
    /// <summary>
    /// Applies the mapping pipeline to the specified stored message.
    /// </summary>
    /// <param name="message">The stored source message.</param>
    /// <param name="mapping">The mapping to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the routing decision and any resulting forwarding job have been persisted.</returns>
    public async Task RouteMessageAsync(
        StoredMessage message,
        ChannelMapping mapping,
        CancellationToken cancellationToken = default)
    {
        FilterVersion? filterVersion = await GetFilterVersionAsync(mapping, cancellationToken);
        RewriteVersion? rewriteVersion = await GetRewriteVersionAsync(mapping, cancellationToken);
        NormalizedTelegramMessage normalizedMessage = NormalizedTelegramMessage.FromEntity(message);
        FilterDecision decision = filterVersion is null
            ? new FilterDecision(true, null, ["No filter set configured."], "Accepted by default")
            : filterEvaluator.Evaluate(normalizedMessage, filterVersion.FilterDefinition);
        RewriteResult? rewrite = rewriteVersion is null
            ? null
            : messageRewriter.Rewrite(normalizedMessage, rewriteVersion.Rules);

        MessageDecision decisionEntity = new()
        {
            MessageId = message.Id,
            MappingId = mapping.Id,
            FilterVersionId = filterVersion?.Id,
            RewriteVersionId = rewriteVersion?.Id,
            Decision = decision.Accepted ? DecisionKind.Accepted : DecisionKind.Rejected,
            MatchedRuleId = decision.MatchedRuleId,
            TraceJson = JsonSerializer.Serialize(decision.Trace),
            RewritePreview = rewrite?.Text ?? rewrite?.Caption,
        };

        dbContext.MessageDecisions.Add(decisionEntity);
        ForwardingJob? forwardingJob = null;
        if (decision.Accepted)
        {
            forwardingJob = new ForwardingJob
            {
                MessageId = message.Id,
                MappingId = mapping.Id,
                FilterVersionId = filterVersion?.Id,
                RewriteVersionId = rewriteVersion?.Id,
                ForwardingMode = mapping.ForwardingMode,
                Status = ForwardingJobStatus.Pending,
            };
            dbContext.ForwardingJobs.Add(forwardingJob);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            DetachIfTracked(decisionEntity);
            DetachIfTracked(forwardingJob);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    private void DetachIfTracked(object? entity)
    {
        if (entity is null)
        {
            return;
        }

        EntityEntry entry = dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task<FilterVersion?> GetFilterVersionAsync(
        ChannelMapping mapping,
        CancellationToken cancellationToken)
    {
        return mapping.ActiveFilterSetId.HasValue
            ? await dbContext.FilterVersions
                .Where(x => x.FilterSetId == mapping.ActiveFilterSetId.Value)
                .OrderByDescending(x => x.Status == FilterVersionStatus.Published)
                .ThenByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
    }

    private async Task<RewriteVersion?> GetRewriteVersionAsync(
        ChannelMapping mapping,
        CancellationToken cancellationToken)
    {
        return mapping.ActiveRewriteSetId.HasValue
            ? await dbContext.RewriteVersions
                .Where(x => x.RewriteSetId == mapping.ActiveRewriteSetId.Value)
                .OrderByDescending(x => x.Status == RewriteVersionStatus.Published)
                .ThenByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
    }
}
