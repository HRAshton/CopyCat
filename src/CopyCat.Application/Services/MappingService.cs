using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Services;

/// <summary>
/// Manages channel mappings and per-mapping filter/rewrite rules.
/// </summary>
internal sealed class MappingService(
    IMappingStore mappingStore,
    IMappingRuleEditor mappingRuleEditor,
    ITelegramControlOperationScheduler controlOperationScheduler,
    IAuditLogService auditLogService) : IMappingManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MappingSummary>> GetMappingsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChannelMapping> mappings = await mappingStore.ListAsync(cancellationToken);
        return mappings
            .OrderByDescending(x => x.UpdatedAt)
            .Select(ToSummary)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MappingSummary> UpsertMappingAsync(
        MappingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceChannelId == request.TargetChannelId)
        {
            throw new DomainConflictException("Source and target channels must be different.");
        }

        if (await mappingStore.ExistsAsync(
                request.SourceChannelId,
                request.TargetChannelId,
                request.Id,
                cancellationToken))
        {
            throw new DomainConflictException("A mapping for the selected source and target channels already exists.");
        }

        ChannelMapping entity = request.Id.HasValue
            ? await mappingStore.GetAsync(request.Id.Value, cancellationToken)
            : new ChannelMapping();
        if (request.Id is null)
        {
            mappingStore.Add(entity);
        }

        entity.SourceChannelId = request.SourceChannelId;
        entity.TargetChannelId = request.TargetChannelId;
        entity.IsEnabled = request.IsEnabled;
        entity.DefaultPolicy = request.DefaultPolicy;
        entity.ForwardingMode = request.ForwardingMode;
        entity.ActiveFilterSetId = request.ActiveFilterSetId;
        entity.ActiveRewriteSetId = request.ActiveRewriteSetId;
        entity.LiveForwardingEnabled = request.LiveForwardingEnabled;
        entity.BackfillCount = request.BackfillCount;

        await mappingStore.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties so that channel titles are available.
        ChannelMapping saved = await mappingStore.GetAsync(entity.Id, cancellationToken);
        await auditLogService.WriteAsync(
            "mapping.saved",
            nameof(ChannelMapping),
            saved.Id,
            null,
            saved,
            cancellationToken);
        return ToSummary(saved);
    }

    /// <inheritdoc />
    public Task<MappingFilterEditorModel> GetMappingFilterEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.GetFilterEditorAsync(mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveMappingFilterAsync(
        Guid mappingId,
        MappingFilterEditorModel model,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.SaveFilterAsync(mappingId, model, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveMappingFilterAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.RemoveFilterAsync(mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FilterDebugResult>> DebugMappingFilterAsync(
        Guid mappingId,
        FilterSetDefinition definition,
        int take,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.DebugFilterAsync(mappingId, definition, take, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MappingRewriteEditorModel> GetMappingRewriteEditorAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.GetRewriteEditorAsync(mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveMappingRewriteAsync(
        Guid mappingId,
        MappingRewriteEditorModel model,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.SaveRewriteAsync(mappingId, model, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveMappingRewriteAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.RemoveRewriteAsync(mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RewriteDebugResult>> DebugMappingRewriteAsync(
        Guid mappingId,
        RewriteRuleSet rules,
        int take,
        CancellationToken cancellationToken = default)
    {
        return mappingRuleEditor.DebugRewriteAsync(mappingId, rules, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RunBackfillAsync(Guid mappingId, int take, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            throw new InvalidDomainOperationException("Backfill count must be greater than zero.");
        }

        ChannelMapping mapping = await mappingStore.GetAsync(mappingId, cancellationToken);
        TelegramSession session = await mappingStore.GetSourceSessionAsync(mappingId, cancellationToken);
        await controlOperationScheduler.EnqueueAsync(
            new TelegramControlOperation
            {
                SessionId = session.Id,
                SourceChannelId = mapping.SourceChannelId,
                MappingId = mappingId,
                OperationType = TelegramControlOperationType.RunBackfill,
                PayloadJson = JsonSerializer.Serialize(new RunBackfillPayload(take), JsonOptions),
            },
            cancellationToken);
        await auditLogService.WriteAsync(
            "mapping.backfill-queued",
            nameof(TelegramControlOperation),
            null,
            null,
            new { mappingId, Requested = take },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        ChannelMapping mapping = await mappingStore.GetAsync(mappingId, cancellationToken);
        Guid? filterSetId = mapping.ActiveFilterSetId;
        Guid? rewriteSetId = mapping.ActiveRewriteSetId;
        mappingStore.Remove(mapping);
        await mappingStore.SaveChangesAsync(cancellationToken);
        if (filterSetId.HasValue)
        {
            await mappingRuleEditor.DeleteFilterSetIfOrphanedAsync(filterSetId.Value, cancellationToken);
        }

        if (rewriteSetId.HasValue)
        {
            await mappingRuleEditor.DeleteRewriteSetIfOrphanedAsync(rewriteSetId.Value, cancellationToken);
        }

        await auditLogService.WriteAsync(
            "mapping.deleted",
            nameof(ChannelMapping),
            mappingId,
            null,
            new { Id = mappingId },
            cancellationToken);
    }

    private static MappingSummary ToSummary(ChannelMapping x)
    {
        return new MappingSummary(
            x.Id,
            x.SourceChannelId,
            x.SourceChannel.Title,
            x.TargetChannelId,
            x.TargetChannel.Title,
            x.IsEnabled,
            x.ForwardingMode,
            x.LiveForwardingEnabled,
            x.ActiveFilterSetId.HasValue,
            x.ActiveRewriteSetId.HasValue,
            x.BackfillCount);
    }
}
