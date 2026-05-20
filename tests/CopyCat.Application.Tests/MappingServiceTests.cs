using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Tests;

public sealed class MappingServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task UpsertMappingAsync_WhenSourceAndTargetMatch_Throws()
    {
        Guid channelId = Guid.NewGuid();
        MappingService sut = CreateService(new StubMappingStore(), new StubMappingRuleEditor(), new StubScheduler(), new StubAuditLogService());

        DomainConflictException exception = await Assert.ThrowsAsync<DomainConflictException>(() =>
            sut.UpsertMappingAsync(new MappingUpsertRequest(null, channelId, channelId, true, MappingDefaultPolicy.Allow, ForwardingMode.CopyAsIs, null, null, true, 100)));

        Assert.Contains("must be different", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpsertMappingAsync_WhenDuplicateExists_Throws()
    {
        StubMappingStore store = new() { Exists = true };
        MappingService sut = CreateService(store, new StubMappingRuleEditor(), new StubScheduler(), new StubAuditLogService());

        DomainConflictException exception = await Assert.ThrowsAsync<DomainConflictException>(() =>
            sut.UpsertMappingAsync(new MappingUpsertRequest(null, Guid.NewGuid(), Guid.NewGuid(), true, MappingDefaultPolicy.Allow, ForwardingMode.NativeForward, null, null, true, 100)));

        Assert.Contains("already exists", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpsertMappingAsync_NewMapping_AddsPersistsAndAudits()
    {
        ChannelMapping mapping = new()
        {
            SourceChannel = new TelegramChannel { Title = "Source" },
            TargetChannel = new TelegramChannel { Title = "Target" },
        };
        StubMappingStore store = new() { Mapping = mapping };
        StubAuditLogService audit = new();
        MappingService sut = CreateService(store, new StubMappingRuleEditor(), new StubScheduler(), audit);

        MappingSummary summary = await sut.UpsertMappingAsync(
            new MappingUpsertRequest(null, Guid.NewGuid(), Guid.NewGuid(), true, MappingDefaultPolicy.Reject, ForwardingMode.CopyWithRewriting, Guid.NewGuid(), Guid.NewGuid(), false, 250));

        Assert.True(store.AddCalled);
        Assert.Equal(250, summary.BackfillCount);
        Assert.Equal("mapping.saved", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task GetMappingsAsync_OrdersByMostRecentlyUpdated()
    {
        ChannelMapping older = new()
        {
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            SourceChannel = new TelegramChannel { Title = "Older Source" },
            TargetChannel = new TelegramChannel { Title = "Older Target" },
        };
        ChannelMapping newer = new()
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            SourceChannel = new TelegramChannel { Title = "Newer Source" },
            TargetChannel = new TelegramChannel { Title = "Newer Target" },
        };
        StubMappingStore store = new() { Mappings = [older, newer], Mapping = newer };
        MappingService sut = CreateService(store, new StubMappingRuleEditor(), new StubScheduler(), new StubAuditLogService());

        IReadOnlyList<MappingSummary> result = await sut.GetMappingsAsync();

        Assert.Equal(["Newer Source", "Older Source"], result.Select(x => x.SourceChannelTitle).ToArray());
    }

    [Fact]
    public async Task UpsertMappingAsync_ExistingMapping_UpdatesWithoutAddingDuplicate()
    {
        ChannelMapping existing = new()
        {
            SourceChannel = new TelegramChannel { Title = "Old Source" },
            TargetChannel = new TelegramChannel { Title = "Old Target" },
        };
        StubMappingStore store = new() { Mapping = existing };
        MappingService sut = CreateService(store, new StubMappingRuleEditor(), new StubScheduler(), new StubAuditLogService());

        MappingSummary summary = await sut.UpsertMappingAsync(
            new MappingUpsertRequest(existing.Id, Guid.NewGuid(), Guid.NewGuid(), false, MappingDefaultPolicy.Allow, ForwardingMode.TextOnly, null, null, false, 20));

        Assert.False(store.AddCalled);
        Assert.Equal(ForwardingMode.TextOnly, existing.ForwardingMode);
        Assert.False(existing.IsEnabled);
        Assert.Equal(20, summary.BackfillCount);
    }

    [Fact]
    public async Task RunBackfillAsync_WithInvalidCount_Throws()
    {
        MappingService sut = CreateService(new StubMappingStore(), new StubMappingRuleEditor(), new StubScheduler(), new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.RunBackfillAsync(Guid.NewGuid(), 0));

        Assert.Contains("greater than zero", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBackfillAsync_QueuesOperationAndAudits()
    {
        ChannelMapping mapping = new() { SourceChannelId = Guid.NewGuid() };
        TelegramSession session = new() { Status = TelegramSessionStatus.Connected };
        StubMappingStore store = new() { Mapping = mapping, SourceSession = session };
        StubScheduler scheduler = new();
        StubAuditLogService audit = new();
        MappingService sut = CreateService(store, new StubMappingRuleEditor(), scheduler, audit);

        await sut.RunBackfillAsync(mapping.Id, 25);

        TelegramControlOperation operation = Assert.Single(scheduler.Enqueued);
        RunBackfillPayload payload = JsonSerializer.Deserialize<RunBackfillPayload>(
            operation.PayloadJson!,
            JsonOptions)!;
        Assert.Equal(mapping.SourceChannelId, operation.SourceChannelId);
        Assert.Equal(TelegramControlOperationType.RunBackfill, operation.OperationType);
        Assert.Equal(25, payload.Take);
        Assert.Equal("mapping.backfill-queued", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task DeleteMappingAsync_RemovesMappingAndCleansOrphans()
    {
        ChannelMapping mapping = new() { ActiveFilterSetId = Guid.NewGuid(), ActiveRewriteSetId = Guid.NewGuid() };
        StubMappingStore store = new() { Mapping = mapping };
        StubMappingRuleEditor editor = new();
        StubAuditLogService audit = new();
        MappingService sut = CreateService(store, editor, new StubScheduler(), audit);

        await sut.DeleteMappingAsync(mapping.Id);

        Assert.Same(mapping, store.RemovedMapping);
        Assert.Equal(mapping.ActiveFilterSetId, editor.DeletedFilterSetId);
        Assert.Equal(mapping.ActiveRewriteSetId, editor.DeletedRewriteSetId);
        Assert.Equal("mapping.deleted", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task MappingRuleOperations_DelegateToEditor()
    {
        Guid mappingId = Guid.NewGuid();
        FilterSetDefinition filterDefinition = new("Inline", MappingDefaultPolicy.Allow, null);
        MappingFilterEditorModel filterModel = new(true, MappingDefaultPolicy.Reject, filterDefinition);
        RewriteRuleSet rewriteRules = new([new AppendFooterOperation("Footer")]);
        MappingRewriteEditorModel rewriteModel = new(true, rewriteRules);
        StubMappingRuleEditor editor = new();
        MappingService sut = CreateService(new StubMappingStore(), editor, new StubScheduler(), new StubAuditLogService());

        MappingFilterEditorModel loadedFilter = await sut.GetMappingFilterEditorAsync(mappingId);
        await sut.SaveMappingFilterAsync(mappingId, filterModel);
        await sut.RemoveMappingFilterAsync(mappingId);
        IReadOnlyList<FilterDebugResult> filterResults = await sut.DebugMappingFilterAsync(mappingId, filterDefinition, 3);
        MappingRewriteEditorModel loadedRewrite = await sut.GetMappingRewriteEditorAsync(mappingId);
        await sut.SaveMappingRewriteAsync(mappingId, rewriteModel);
        await sut.RemoveMappingRewriteAsync(mappingId);
        IReadOnlyList<RewriteDebugResult> rewriteResults = await sut.DebugMappingRewriteAsync(mappingId, rewriteRules, 2);

        Assert.Equal(mappingId, editor.LastFilterEditorMappingId);
        Assert.Equal(mappingId, editor.SavedFilterMappingId);
        Assert.Equal(mappingId, editor.RemovedFilterMappingId);
        Assert.Equal(mappingId, editor.DebugFilterMappingId);
        Assert.Equal(filterModel, editor.SavedFilterModel);
        Assert.Equal(filterDefinition, editor.DebugFilterDefinition);
        Assert.Equal(3, editor.DebugFilterTake);
        Assert.Same(editor.FilterEditorResult, loadedFilter);
        Assert.Same(editor.FilterDebugResults, filterResults);
        Assert.Equal(mappingId, editor.LastRewriteEditorMappingId);
        Assert.Equal(mappingId, editor.SavedRewriteMappingId);
        Assert.Equal(mappingId, editor.RemovedRewriteMappingId);
        Assert.Equal(mappingId, editor.DebugRewriteMappingId);
        Assert.Equal(rewriteModel, editor.SavedRewriteModel);
        Assert.Equal(rewriteRules, editor.DebugRewriteRules);
        Assert.Equal(2, editor.DebugRewriteTake);
        Assert.Same(editor.RewriteEditorResult, loadedRewrite);
        Assert.Same(editor.RewriteDebugResults, rewriteResults);
    }

    private static MappingService CreateService(
        StubMappingStore store,
        StubMappingRuleEditor editor,
        StubScheduler scheduler,
        StubAuditLogService audit)
    {
        return new MappingService(store, editor, scheduler, audit);
    }

    private sealed class StubMappingStore : IMappingStore
    {
        public IReadOnlyList<ChannelMapping> Mappings { get; init; }
            = [new() { SourceChannel = new TelegramChannel { Title = "Source" }, TargetChannel = new TelegramChannel { Title = "Target" } }];

        public ChannelMapping Mapping { get; set; } = new()
        {
            SourceChannel = new TelegramChannel { Title = "Source" },
            TargetChannel = new TelegramChannel { Title = "Target" },
        };

        public TelegramSession SourceSession { get; init; } = new() { Status = TelegramSessionStatus.Connected };

        public bool Exists { get; init; }

        public bool AddCalled { get; private set; }

        public ChannelMapping? RemovedMapping { get; private set; }

        public Task<IReadOnlyList<ChannelMapping>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mappings);
        }

        public Task<ChannelMapping> GetAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mapping);
        }

        public Task<bool> ExistsAsync(Guid sourceChannelId, Guid targetChannelId, Guid? excludeMappingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Exists);
        }

        public void Add(ChannelMapping mapping)
        {
            AddCalled = true;
            mapping.SourceChannel = new TelegramChannel { Title = "Source" };
            mapping.TargetChannel = new TelegramChannel { Title = "Target" };
            Mapping = mapping;
        }

        public void Remove(ChannelMapping mapping)
        {
            RemovedMapping = mapping;
        }

        public Task<TelegramSession> GetSourceSessionAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SourceSession);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubMappingRuleEditor : IMappingRuleEditor
    {
        public MappingFilterEditorModel FilterEditorResult { get; } =
            new(true, MappingDefaultPolicy.Allow, new FilterSetDefinition("Default", MappingDefaultPolicy.Allow, null));

        public IReadOnlyList<FilterDebugResult> FilterDebugResults { get; } = [];

        public MappingRewriteEditorModel RewriteEditorResult { get; } =
            new(true, new RewriteRuleSet([new AppendFooterOperation("Footer")]));

        public IReadOnlyList<RewriteDebugResult> RewriteDebugResults { get; } = [];

        public Guid? DeletedFilterSetId { get; private set; }

        public Guid? DeletedRewriteSetId { get; private set; }

        public Guid? LastFilterEditorMappingId { get; private set; }

        public Guid? SavedFilterMappingId { get; private set; }

        public MappingFilterEditorModel? SavedFilterModel { get; private set; }

        public Guid? RemovedFilterMappingId { get; private set; }

        public Guid? DebugFilterMappingId { get; private set; }

        public FilterSetDefinition? DebugFilterDefinition { get; private set; }

        public int DebugFilterTake { get; private set; }

        public Guid? LastRewriteEditorMappingId { get; private set; }

        public Guid? SavedRewriteMappingId { get; private set; }

        public MappingRewriteEditorModel? SavedRewriteModel { get; private set; }

        public Guid? RemovedRewriteMappingId { get; private set; }

        public Guid? DebugRewriteMappingId { get; private set; }

        public RewriteRuleSet? DebugRewriteRules { get; private set; }

        public int DebugRewriteTake { get; private set; }

        public Task<MappingFilterEditorModel> GetFilterEditorAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            LastFilterEditorMappingId = mappingId;
            return Task.FromResult(FilterEditorResult);
        }

        public Task SaveFilterAsync(Guid mappingId, MappingFilterEditorModel model, CancellationToken cancellationToken = default)
        {
            SavedFilterMappingId = mappingId;
            SavedFilterModel = model;
            return Task.CompletedTask;
        }

        public Task RemoveFilterAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            RemovedFilterMappingId = mappingId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FilterDebugResult>> DebugFilterAsync(Guid mappingId, FilterSetDefinition definition, int take, CancellationToken cancellationToken = default)
        {
            DebugFilterMappingId = mappingId;
            DebugFilterDefinition = definition;
            DebugFilterTake = take;
            return Task.FromResult(FilterDebugResults);
        }

        public Task<MappingRewriteEditorModel> GetRewriteEditorAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            LastRewriteEditorMappingId = mappingId;
            return Task.FromResult(RewriteEditorResult);
        }

        public Task SaveRewriteAsync(Guid mappingId, MappingRewriteEditorModel model, CancellationToken cancellationToken = default)
        {
            SavedRewriteMappingId = mappingId;
            SavedRewriteModel = model;
            return Task.CompletedTask;
        }

        public Task RemoveRewriteAsync(Guid mappingId, CancellationToken cancellationToken = default)
        {
            RemovedRewriteMappingId = mappingId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RewriteDebugResult>> DebugRewriteAsync(Guid mappingId, RewriteRuleSet rules, int take, CancellationToken cancellationToken = default)
        {
            DebugRewriteMappingId = mappingId;
            DebugRewriteRules = rules;
            DebugRewriteTake = take;
            return Task.FromResult(RewriteDebugResults);
        }

        public Task DeleteFilterSetIfOrphanedAsync(Guid filterSetId, CancellationToken cancellationToken = default)
        {
            DeletedFilterSetId = filterSetId;
            return Task.CompletedTask;
        }

        public Task DeleteRewriteSetIfOrphanedAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
        {
            DeletedRewriteSetId = rewriteSetId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubScheduler : ITelegramControlOperationScheduler
    {
        public List<TelegramControlOperation> Enqueued { get; } = [];

        public Task<bool> HasPendingAsync(Guid sessionId, TelegramControlOperationType operationType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task EnqueueAsync(TelegramControlOperation operation, CancellationToken cancellationToken = default)
        {
            Enqueued.Add(operation);
            return Task.CompletedTask;
        }
    }

    private sealed class StubAuditLogService : IAuditLogService
    {
        public List<(string Action, string EntityType, Guid? EntityId, object? Before, object? After)> Entries { get; } = [];

        public Task<IReadOnlyList<AuditLogItem>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AuditLogItem>>([]);
        }

        public Task WriteAsync(string action, string entityType, Guid? entityId, object? before, object? after, CancellationToken cancellationToken = default)
        {
            Entries.Add((action, entityType, entityId, before, after));
            return Task.CompletedTask;
        }
    }
}
