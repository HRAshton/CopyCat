using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Messages;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates filter set administration use cases.
/// </summary>
internal sealed class ApplicationFilterManagementService(
    IFilterSetStore filterSetStore,
    IFilterEvaluator filterEvaluator,
    IAuditLogService auditLogService) : IFilterManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FilterSetSummary>> GetFilterSetsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FilterSet> sets = await filterSetStore.ListAsync(cancellationToken);
        return sets.Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<FilterSetSummary> SaveDraftAsync(
        FilterSetEditorModel model,
        CancellationToken cancellationToken = default)
    {
        FilterSet filterSet = model.FilterSetId.HasValue
            ? await filterSetStore.GetAsync(model.FilterSetId.Value, cancellationToken)
            : filterSetStore.Create();
        int nextVersionNumber = model.FilterSetId.HasValue
            ? await filterSetStore.GetNextVersionNumberAsync(filterSet.Id, cancellationToken)
            : 1;

        filterSet.Name = model.Name;
        filterSet.Description = model.Description;

        FilterVersion newVersion = new()
        {
            FilterSetId = filterSet.Id,
            VersionNumber = nextVersionNumber,
            Status = FilterVersionStatus.Draft,
            FilterDefinition = model.Definition,
        };

        filterSetStore.AddVersion(newVersion);
        await filterSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "filter.saved",
            nameof(FilterSet),
            filterSet.Id,
            null,
            new
            {
                filterSet.Id,
                filterSet.Name,
                filterSet.Description,
                VersionId = newVersion.Id,
                newVersion.VersionNumber,
                newVersion.Status,
            },
            cancellationToken);

        FilterSet saved = await filterSetStore.GetAsync(filterSet.Id, cancellationToken);
        return ToSummary(saved);
    }

    /// <inheritdoc />
    public async Task PublishLatestAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        FilterSet filterSet = await filterSetStore.GetAsync(filterSetId, cancellationToken);
        foreach (FilterVersion version in filterSet.Versions)
        {
            version.Status = FilterVersionStatus.Archived;
        }

        FilterVersion latest = filterSet.Versions.OrderByDescending(x => x.VersionNumber).First();
        latest.Status = FilterVersionStatus.Published;
        latest.PublishedAt = DateTimeOffset.UtcNow;
        await filterSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "filter.published",
            nameof(FilterSet),
            filterSet.Id,
            null,
            new
            {
                filterSet.Id,
                LatestVersionId = latest.Id,
                latest.VersionNumber,
                latest.Status,
                latest.PublishedAt,
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilterDebugResult>> DebugAsync(
        Guid filterSetId,
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default)
    {
        FilterSet filterSet = await filterSetStore.GetAsync(filterSetId, cancellationToken);
        FilterVersion version = filterSet.Versions
            .OrderByDescending(x => x.Status == FilterVersionStatus.Published)
            .ThenByDescending(x => x.VersionNumber)
            .First();
        IReadOnlyList<StoredMessage> messages =
            await filterSetStore.GetRecentMessagesAsync(channelId, take, cancellationToken);

        return messages.Select(message =>
        {
            NormalizedTelegramMessage normalized = NormalizedTelegramMessage.FromEntity(message);
            FilterDecision decision = filterEvaluator.Evaluate(normalized, version.FilterDefinition);
            return new FilterDebugResult(
                message.TelegramMessageId,
                message.MessageDate,
                message.Text ?? message.Caption ?? string.Empty,
                decision.Accepted,
                decision.MatchedRuleId,
                decision.Trace,
                RewritePreview: null);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteFilterSetAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        bool inUse = await filterSetStore.IsInUseAsync(filterSetId, cancellationToken);
        if (inUse)
        {
            throw new InvalidDomainOperationException("This filter set is still attached to one or more mappings.");
        }

        FilterSet filterSet = await filterSetStore.GetAsync(filterSetId, cancellationToken);
        filterSetStore.RemoveVersions(filterSet.Versions);
        filterSetStore.Remove(filterSet);
        await filterSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "filter.deleted",
            nameof(FilterSet),
            filterSetId,
            null,
            new { Id = filterSetId },
            cancellationToken);
    }

    private static FilterSetSummary ToSummary(FilterSet set)
    {
        FilterVersion? latest = set.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        return new FilterSetSummary(
            set.Id,
            set.Name,
            set.Description,
            set.Versions.Count,
            latest?.FilterDefinition);
    }
}
