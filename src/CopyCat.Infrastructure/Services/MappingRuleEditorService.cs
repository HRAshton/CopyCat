using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Messages;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Manages per-mapping inline filter and rewrite rule sets.
/// </summary>
internal sealed class MappingRuleEditorService(
    IMappingRuleEditorStore store,
    IFilterEvaluator filterEvaluator,
    IMessageRewriter messageRewriter,
    IAuditLogService auditLogService) : IMappingRuleEditor
{
    /// <inheritdoc />
    public async Task<MappingFilterEditorModel> GetFilterEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        ChannelMapping mapping = await store.GetMappingWithFilterSetAsync(mappingId, cancellationToken);
        FilterSetDefinition definition = GetLatestFilterDefinition(mapping.ActiveFilterSet);
        return new MappingFilterEditorModel(mapping.ActiveFilterSetId.HasValue, definition.DefaultPolicy, definition);
    }

    /// <inheritdoc />
    public async Task SaveFilterAsync(
        Guid mappingId,
        MappingFilterEditorModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.IsEnabled)
        {
            await RemoveFilterAsync(mappingId, cancellationToken);
            return;
        }

        ChannelMapping mapping = await store.GetMappingWithChannelsAndFilterSetAsync(mappingId, cancellationToken);
        FilterSet filterSet = mapping.ActiveFilterSet ?? new FilterSet();
        if (mapping.ActiveFilterSet is null)
        {
            store.AddFilterSet(filterSet);
            mapping.ActiveFilterSet = filterSet;
            mapping.ActiveFilterSetId = filterSet.Id;
        }

        filterSet.Name = $"Mapping {mapping.SourceChannel.Title} -> {mapping.TargetChannel.Title} Filter";
        filterSet.Description =
            $"Inline filter for mapping {mapping.SourceChannel.Title} -> {mapping.TargetChannel.Title}";

        foreach (FilterVersion version in filterSet.Versions)
        {
            version.Status = FilterVersionStatus.Archived;
        }

        int nextVersionNumber = filterSet.Versions.Count == 0 ? 1 : filterSet.Versions.Max(x => x.VersionNumber) + 1;
        FilterVersion newVersion = new()
        {
            FilterSet = filterSet,
            VersionNumber = nextVersionNumber,
            Status = FilterVersionStatus.Published,
            FilterDefinition = model.Definition with { DefaultPolicy = model.DefaultPolicy },
            PublishedAt = DateTimeOffset.UtcNow,
        };

        store.AddFilterVersion(newVersion);
        await store.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "mapping.filter-saved",
            nameof(ChannelMapping),
            mappingId,
            null,
            new { mappingId, FilterSetId = filterSet.Id, newVersion.VersionNumber, newVersion.Status },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveFilterAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        ChannelMapping mapping = await store.GetMappingAsync(mappingId, cancellationToken);
        Guid? filterSetId = mapping.ActiveFilterSetId;
        mapping.ActiveFilterSetId = null;
        mapping.ActiveFilterSet = null;
        await store.SaveChangesAsync(cancellationToken);
        if (filterSetId.HasValue)
        {
            await DeleteFilterSetIfOrphanedAsync(filterSetId.Value, cancellationToken);
        }

        await auditLogService.WriteAsync(
            "mapping.filter-removed",
            nameof(ChannelMapping),
            mappingId,
            null,
            new { mappingId, RemovedFilterSetId = filterSetId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilterDebugResult>> DebugFilterAsync(
        Guid mappingId,
        FilterSetDefinition definition,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            throw new InvalidDomainOperationException("Debug message count must be greater than zero.");
        }

        ChannelMapping mapping = await store.GetMappingWithRewriteSetAsync(mappingId, cancellationToken);
        RewriteRuleSet? rewriteRules = GetLatestRewriteRules(mapping.ActiveRewriteSet);
        IReadOnlyList<StoredMessage> messages =
            await store.GetRecentSourceMessagesAsync(mapping.SourceChannelId, take, cancellationToken);

        return messages.Select(message =>
        {
            NormalizedTelegramMessage normalized = NormalizedTelegramMessage.FromEntity(message);
            FilterDecision decision = filterEvaluator.Evaluate(normalized, definition);
            string? rewritePreview =
                rewriteRules is null ? null : messageRewriter.Rewrite(normalized, rewriteRules).Text;
            return new FilterDebugResult(
                message.TelegramMessageId,
                message.MessageDate,
                message.Text ?? message.Caption ?? string.Empty,
                decision.Accepted,
                decision.MatchedRuleId,
                decision.Trace,
                rewritePreview);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<MappingRewriteEditorModel> GetRewriteEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        ChannelMapping mapping = await store.GetMappingWithRewriteSetAsync(mappingId, cancellationToken);
        RewriteRuleSet rules = GetLatestRewriteRules(mapping.ActiveRewriteSet) ?? new RewriteRuleSet();
        return new MappingRewriteEditorModel(mapping.ActiveRewriteSetId.HasValue, rules);
    }

    /// <inheritdoc />
    public async Task SaveRewriteAsync(
        Guid mappingId,
        MappingRewriteEditorModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.IsEnabled)
        {
            await RemoveRewriteAsync(mappingId, cancellationToken);
            return;
        }

        ChannelMapping mapping = await store.GetMappingWithChannelsAndRewriteSetAsync(mappingId, cancellationToken);
        RewriteSet rewriteSet = mapping.ActiveRewriteSet ?? new RewriteSet();
        if (mapping.ActiveRewriteSet is null)
        {
            store.AddRewriteSet(rewriteSet);
            mapping.ActiveRewriteSet = rewriteSet;
            mapping.ActiveRewriteSetId = rewriteSet.Id;
        }

        rewriteSet.Name = $"Mapping {mapping.SourceChannel.Title} -> {mapping.TargetChannel.Title} Rewrite";
        rewriteSet.Description =
            $"Inline rewrite for mapping {mapping.SourceChannel.Title} -> {mapping.TargetChannel.Title}";

        foreach (RewriteVersion version in rewriteSet.Versions)
        {
            version.Status = RewriteVersionStatus.Archived;
        }

        int nextVersionNumber = rewriteSet.Versions.Count == 0 ? 1 : rewriteSet.Versions.Max(x => x.VersionNumber) + 1;
        RewriteVersion newVersion = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = nextVersionNumber,
            Status = RewriteVersionStatus.Published,
            Rules = model.Rules,
            PublishedAt = DateTimeOffset.UtcNow,
        };

        store.AddRewriteVersion(newVersion);
        await store.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "mapping.rewrite-saved",
            nameof(ChannelMapping),
            mappingId,
            null,
            new { mappingId, RewriteSetId = rewriteSet.Id, newVersion.VersionNumber, newVersion.Status },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveRewriteAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        ChannelMapping mapping = await store.GetMappingAsync(mappingId, cancellationToken);
        Guid? rewriteSetId = mapping.ActiveRewriteSetId;
        mapping.ActiveRewriteSetId = null;
        mapping.ActiveRewriteSet = null;
        await store.SaveChangesAsync(cancellationToken);
        if (rewriteSetId.HasValue)
        {
            await DeleteRewriteSetIfOrphanedAsync(rewriteSetId.Value, cancellationToken);
        }

        await auditLogService.WriteAsync(
            "mapping.rewrite-removed",
            nameof(ChannelMapping),
            mappingId,
            null,
            new { mappingId, RemovedRewriteSetId = rewriteSetId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RewriteDebugResult>> DebugRewriteAsync(
        Guid mappingId,
        RewriteRuleSet rules,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            throw new InvalidDomainOperationException("Debug message count must be greater than zero.");
        }

        ChannelMapping mapping = await store.GetMappingAsync(mappingId, cancellationToken);
        IReadOnlyList<StoredMessage> messages =
            await store.GetRecentSourceMessagesAsync(mapping.SourceChannelId, take, cancellationToken);

        return messages.Select(message =>
        {
            NormalizedTelegramMessage normalized = NormalizedTelegramMessage.FromEntity(message);
            RewriteResult result = messageRewriter.Rewrite(normalized, rules);
            return new RewriteDebugResult(
                message.TelegramMessageId,
                message.MessageDate,
                message.Text ?? message.Caption ?? string.Empty,
                result.Text,
                result.Caption,
                result.DropMedia,
                result.Trace);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteFilterSetIfOrphanedAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        if (await store.IsFilterSetReferencedAsync(filterSetId, cancellationToken))
        {
            return;
        }

        FilterSet? filterSet = await store.FindFilterSetWithVersionsAsync(filterSetId, cancellationToken);
        if (filterSet is null)
        {
            return;
        }

        store.RemoveFilterVersions(filterSet.Versions);
        store.RemoveFilterSet(filterSet);
        await store.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRewriteSetIfOrphanedAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        if (await store.IsRewriteSetReferencedAsync(rewriteSetId, cancellationToken))
        {
            return;
        }

        RewriteSet? rewriteSet = await store.FindRewriteSetWithVersionsAsync(rewriteSetId, cancellationToken);
        if (rewriteSet is null)
        {
            return;
        }

        store.RemoveRewriteVersions(rewriteSet.Versions);
        store.RemoveRewriteSet(rewriteSet);
        await store.SaveChangesAsync(cancellationToken);
    }

    private static FilterSetDefinition GetLatestFilterDefinition(FilterSet? filterSet)
    {
        return filterSet?.Versions
                   .OrderByDescending(x => x.Status == FilterVersionStatus.Published)
                   .ThenByDescending(x => x.VersionNumber)
                   .Select(x => x.FilterDefinition)
                   .FirstOrDefault()
               ?? FilterSetDefinition.AllowAll();
    }

    private static RewriteRuleSet? GetLatestRewriteRules(RewriteSet? rewriteSet)
    {
        return rewriteSet?.Versions
            .OrderByDescending(x => x.Status == RewriteVersionStatus.Published)
            .ThenByDescending(x => x.VersionNumber)
            .Select(x => x.Rules)
            .FirstOrDefault();
    }
}
