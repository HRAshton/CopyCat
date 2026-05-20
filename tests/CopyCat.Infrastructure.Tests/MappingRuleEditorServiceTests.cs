using CopyCat.Application.Abstractions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;
using CopyCat.Infrastructure.Stores;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class MappingRuleEditorServiceTests
{
    [Fact]
    public async Task SaveFilterAsync_CreatesPublishedVersion_AndArchivesPreviousVersionAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(
                true,
                MappingDefaultPolicy.Allow,
                new FilterSetDefinition("Allow text", MappingDefaultPolicy.Allow, new HasTextCondition(true, "text"))));

        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(
                true,
                MappingDefaultPolicy.Reject,
                new FilterSetDefinition(
                    "Reject telegram links",
                    MappingDefaultPolicy.Reject,
                    new HasTelegramLinkCondition(true, "telegram"))));

        ChannelMapping reloadedMapping = await dbContext.ChannelMappings
            .Include(x => x.ActiveFilterSet)
            .ThenInclude(x => x!.Versions)
            .SingleAsync(x => x.Id == mapping.Id);
        FilterSet filterSet = Assert.IsType<FilterSet>(reloadedMapping.ActiveFilterSet);
        Assert.Equal(
            $"Mapping Source -> Target Filter",
            filterSet.Name);
        Assert.Collection(
            filterSet.Versions.OrderBy(x => x.VersionNumber),
            version =>
            {
                Assert.Equal(1, version.VersionNumber);
                Assert.Equal(FilterVersionStatus.Archived, version.Status);
            },
            version =>
            {
                Assert.Equal(2, version.VersionNumber);
                Assert.Equal(FilterVersionStatus.Published, version.Status);
                Assert.Equal(MappingDefaultPolicy.Reject, version.FilterDefinition.DefaultPolicy);
                Assert.IsType<HasTelegramLinkCondition>(version.FilterDefinition.Root);
            });
        Assert.Equal(["mapping.filter-saved", "mapping.filter-saved"], auditLogService.Actions);
    }

    [Fact]
    public async Task RemoveFilterAsync_RemovesMappingReference_AndDeletesOrphanedFilterSetAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);
        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(
                true,
                MappingDefaultPolicy.Allow,
                new FilterSetDefinition("Allow all", MappingDefaultPolicy.Allow, null)));

        Guid filterSetId = (await dbContext.ChannelMappings
                .AsNoTracking()
                .SingleAsync(x => x.Id == mapping.Id))
            .ActiveFilterSetId!.Value;

        await service.RemoveFilterAsync(mapping.Id);

        ChannelMapping reloadedMapping = await dbContext.ChannelMappings.SingleAsync(x => x.Id == mapping.Id);
        Assert.Null(reloadedMapping.ActiveFilterSetId);
        Assert.False(await dbContext.FilterSets.AnyAsync(x => x.Id == filterSetId));
        Assert.Contains("mapping.filter-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task DebugFilterAsync_ReturnsRecentMessages_WithRewritePreviewAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);
        RewriteSet rewriteSet = new();
        RewriteVersion rewriteVersion = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = 1,
            Status = RewriteVersionStatus.Published,
            Rules = new RewriteRuleSet([new AppendFooterOperation("Footer")]),
        };
        mapping.ActiveRewriteSet = rewriteSet;
        mapping.ActiveRewriteSetId = rewriteSet.Id;
        await dbContext.AddRangeAsync(rewriteSet, rewriteVersion);
        await dbContext.Messages.AddRangeAsync(
            CreateMessage(mapping.SourceChannelId, 10, DateTimeOffset.UtcNow.AddMinutes(-5), "older"),
            CreateMessage(mapping.SourceChannelId, 11, DateTimeOffset.UtcNow, "newer"));
        await dbContext.SaveChangesAsync();

        IReadOnlyList<FilterDebugResult> results = await service.DebugFilterAsync(
            mapping.Id,
            new FilterSetDefinition("Has text", MappingDefaultPolicy.Allow, new HasTextCondition(true, "text")),
            2);

        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(11, result.TelegramMessageId);
                Assert.True(result.Accepted);
                Assert.NotNull(result.RewritePreview);
                Assert.Contains("Footer", result.RewritePreview);
            },
            result =>
            {
                Assert.Equal(10, result.TelegramMessageId);
                Assert.True(result.Accepted);
                Assert.NotNull(result.RewritePreview);
                Assert.Contains("Footer", result.RewritePreview);
            });
    }

    [Fact]
    public async Task GetRewriteEditorAsync_ReturnsLatestPublishedRules_AndDisabledStateAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        RewriteSet rewriteSet = new() { Name = "Inline rewrite" };
        RewriteVersion draft = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = 1,
            Status = RewriteVersionStatus.Draft,
            Rules = new RewriteRuleSet([new AppendFooterOperation("Draft")]),
        };
        RewriteVersion published = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = 2,
            Status = RewriteVersionStatus.Published,
            Rules = new RewriteRuleSet([new AppendFooterOperation("Published")]),
        };
        mapping.ActiveRewriteSet = rewriteSet;
        mapping.ActiveRewriteSetId = rewriteSet.Id;
        await dbContext.AddRangeAsync(rewriteSet, draft, published);
        await dbContext.SaveChangesAsync();

        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        ChannelMapping mappingWithoutRewrite = await SeedMappingAsync(dbContext);

        MappingRewriteEditorModel enabled = await service.GetRewriteEditorAsync(mapping.Id);
        MappingRewriteEditorModel disabled = await service.GetRewriteEditorAsync(mappingWithoutRewrite.Id);

        Assert.True(enabled.IsEnabled);
        Assert.Single(enabled.Rules.EffectiveOperations);
        Assert.IsType<AppendFooterOperation>(enabled.Rules.EffectiveOperations[0]);
        Assert.False(disabled.IsEnabled);
        Assert.Empty(disabled.Rules.EffectiveOperations);
    }

    [Fact]
    public async Task SaveRewriteAsync_CreatesPublishedVersion_AndArchivesPreviousVersionAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(
                true,
                new RewriteRuleSet([new AppendFooterOperation("Footer")])));

        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(
                true,
                new RewriteRuleSet([new StripAllTextOperation()])));

        ChannelMapping reloadedMapping = await dbContext.ChannelMappings
            .Include(x => x.ActiveRewriteSet)
            .ThenInclude(x => x!.Versions)
            .SingleAsync(x => x.Id == mapping.Id);
        RewriteSet rewriteSet = Assert.IsType<RewriteSet>(reloadedMapping.ActiveRewriteSet);
        Assert.Equal($"Mapping Source -> Target Rewrite", rewriteSet.Name);
        Assert.Collection(
            rewriteSet.Versions.OrderBy(x => x.VersionNumber),
            version =>
            {
                Assert.Equal(1, version.VersionNumber);
                Assert.Equal(RewriteVersionStatus.Archived, version.Status);
            },
            version =>
            {
                Assert.Equal(2, version.VersionNumber);
                Assert.Equal(RewriteVersionStatus.Published, version.Status);
                Assert.IsType<StripAllTextOperation>(Assert.Single(version.Rules.EffectiveOperations));
            });
        Assert.Equal(["mapping.rewrite-saved", "mapping.rewrite-saved"], auditLogService.Actions);
    }

    [Fact]
    public async Task RemoveRewriteAsync_RemovesMappingReference_AndDeletesOrphanedRewriteSetAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);
        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(
                true,
                new RewriteRuleSet([new AppendFooterOperation("Footer")])));

        Guid rewriteSetId = (await dbContext.ChannelMappings
                .AsNoTracking()
                .SingleAsync(x => x.Id == mapping.Id))
            .ActiveRewriteSetId!.Value;

        await service.RemoveRewriteAsync(mapping.Id);

        ChannelMapping reloadedMapping = await dbContext.ChannelMappings.SingleAsync(x => x.Id == mapping.Id);
        Assert.Null(reloadedMapping.ActiveRewriteSetId);
        Assert.False(await dbContext.RewriteSets.AnyAsync(x => x.Id == rewriteSetId));
        Assert.Contains("mapping.rewrite-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task DebugRewriteAsync_ReturnsRecentMessages_WithTransformedContentAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);
        await dbContext.Messages.AddRangeAsync(
            CreateMessage(mapping.SourceChannelId, 20, DateTimeOffset.UtcNow.AddMinutes(-5), "older"),
            CreateMessage(mapping.SourceChannelId, 21, DateTimeOffset.UtcNow, "newer"));
        await dbContext.SaveChangesAsync();

        IReadOnlyList<RewriteDebugResult> results = await service.DebugRewriteAsync(
            mapping.Id,
            new RewriteRuleSet([new AppendFooterOperation("Footer")]),
            2);

        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(21, result.TelegramMessageId);
                Assert.Contains("Footer", result.RewrittenText);
                Assert.False(result.DropMedia);
            },
            result =>
            {
                Assert.Equal(20, result.TelegramMessageId);
                Assert.Contains("Footer", result.RewrittenText);
                Assert.False(result.DropMedia);
            });
    }

    [Fact]
    public async Task GetFilterEditorAsync_ReturnsDefaultAllowAll_WhenNoFilterSetConfiguredAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        MappingFilterEditorModel model = await service.GetFilterEditorAsync(mapping.Id);

        Assert.False(model.IsEnabled);
        Assert.Equal(MappingDefaultPolicy.Allow, model.DefaultPolicy);
    }

    [Fact]
    public async Task GetFilterEditorAsync_ReturnsLatestPublishedDefinition_WhenFilterSetExistsAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        FilterSetDefinition definition = new(
            "Has text",
            MappingDefaultPolicy.Reject,
            new HasTextCondition(true, "hello"));
        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(true, MappingDefaultPolicy.Reject, definition));

        MappingFilterEditorModel model = await service.GetFilterEditorAsync(mapping.Id);

        Assert.True(model.IsEnabled);
        Assert.Equal(MappingDefaultPolicy.Reject, model.DefaultPolicy);
    }

    [Fact]
    public async Task SaveFilterAsync_WithDisabledModel_RemovesExistingFilterAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(
                true,
                MappingDefaultPolicy.Allow,
                FilterSetDefinition.AllowAll()));
        Guid? filterSetId = (await dbContext.ChannelMappings.FindAsync(mapping.Id))!.ActiveFilterSetId;
        Assert.NotNull(filterSetId);

        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(false, MappingDefaultPolicy.Allow, FilterSetDefinition.AllowAll()));

        ChannelMapping reloaded = await dbContext.ChannelMappings.FindAsync(mapping.Id) ??
                                  throw new InvalidOperationException();
        Assert.Null(reloaded.ActiveFilterSetId);
        Assert.Contains("mapping.filter-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task SaveRewriteAsync_WithDisabledModel_RemovesExistingRewriteAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(true, new RewriteRuleSet([new AppendFooterOperation("Footer")])));
        Guid? rewriteSetId = (await dbContext.ChannelMappings.FindAsync(mapping.Id))!.ActiveRewriteSetId;
        Assert.NotNull(rewriteSetId);

        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(false, new RewriteRuleSet([])));

        ChannelMapping reloaded = await dbContext.ChannelMappings.FindAsync(mapping.Id) ??
                                  throw new InvalidOperationException();
        Assert.Null(reloaded.ActiveRewriteSetId);
        Assert.Contains("mapping.rewrite-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task DebugFilterAsync_ThrowsInvalidDomainOperationException_WhenTakeIsZeroAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await Assert.ThrowsAsync<Application.Exceptions.InvalidDomainOperationException>(() =>
            service.DebugFilterAsync(mapping.Id, FilterSetDefinition.AllowAll(), 0));
    }

    [Fact]
    public async Task DebugRewriteAsync_ThrowsInvalidDomainOperationException_WhenTakeIsNegativeAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await Assert.ThrowsAsync<Application.Exceptions.InvalidDomainOperationException>(() =>
            service.DebugRewriteAsync(mapping.Id, new RewriteRuleSet([]), -1));
    }

    [Fact]
    public async Task DeleteFilterSetIfOrphanedAsync_SkipsDeletion_WhenFilterSetIsStillReferencedAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveFilterAsync(
            mapping.Id,
            new MappingFilterEditorModel(true, MappingDefaultPolicy.Allow, FilterSetDefinition.AllowAll()));

        Guid filterSetId = (await dbContext.ChannelMappings.FindAsync(mapping.Id))!.ActiveFilterSetId!.Value;

        // Filter set is still referenced by the mapping – should not be deleted.
        await service.DeleteFilterSetIfOrphanedAsync(filterSetId);

        Assert.True(await dbContext.FilterSets.AnyAsync(x => x.Id == filterSetId));
    }

    [Fact]
    public async Task DeleteFilterSetIfOrphanedAsync_SkipsDeletion_WhenFilterSetDoesNotExistAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        // Should not throw even though the filter set does not exist.
        await service.DeleteFilterSetIfOrphanedAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task DeleteRewriteSetIfOrphanedAsync_SkipsDeletion_WhenRewriteSetIsStillReferencedAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.SaveRewriteAsync(
            mapping.Id,
            new MappingRewriteEditorModel(true, new RewriteRuleSet([new AppendFooterOperation("Footer")])));

        Guid rewriteSetId = (await dbContext.ChannelMappings.FindAsync(mapping.Id))!.ActiveRewriteSetId!.Value;

        // Rewrite set is still referenced by the mapping – should not be deleted.
        await service.DeleteRewriteSetIfOrphanedAsync(rewriteSetId);

        Assert.True(await dbContext.RewriteSets.AnyAsync(x => x.Id == rewriteSetId));
    }

    [Fact]
    public async Task DeleteRewriteSetIfOrphanedAsync_SkipsDeletion_WhenRewriteSetDoesNotExistAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        // Should not throw even though the rewrite set does not exist.
        await service.DeleteRewriteSetIfOrphanedAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task RemoveFilterAsync_WhenNoFilterSetLinked_WritesAuditLogAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.RemoveFilterAsync(mapping.Id);

        ChannelMapping reloaded = await dbContext.ChannelMappings.FindAsync(mapping.Id) ??
                                  throw new InvalidOperationException();
        Assert.Null(reloaded.ActiveFilterSetId);
        Assert.Contains("mapping.filter-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task RemoveRewriteAsync_WhenNoRewriteSetLinked_WritesAuditLogAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await service.RemoveRewriteAsync(mapping.Id);

        ChannelMapping reloaded = await dbContext.ChannelMappings.FindAsync(mapping.Id) ??
                                  throw new InvalidOperationException();
        Assert.Null(reloaded.ActiveRewriteSetId);
        Assert.Contains("mapping.rewrite-removed", auditLogService.Actions);
    }

    [Fact]
    public async Task DebugFilterAsync_WhenNoRewriteSet_ReturnsNullRewritePreviewAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        RecordingAuditLogService auditLogService = new();
        ChannelMapping mapping = await SeedMappingAsync(dbContext);
        MappingRuleEditorService service = CreateService(dbContext, auditLogService);

        await dbContext.Messages.AddAsync(
            CreateMessage(mapping.SourceChannelId, 30, DateTimeOffset.UtcNow, "hello text"));
        await dbContext.SaveChangesAsync();

        IReadOnlyList<FilterDebugResult> results = await service.DebugFilterAsync(
            mapping.Id,
            FilterSetDefinition.AllowAll(),
            1);

        FilterDebugResult result = Assert.Single(results);
        Assert.True(result.Accepted);
        Assert.Null(result.RewritePreview);
    }

    private static MappingRuleEditorService CreateService(
        CopyCatDbContext dbContext,
        IAuditLogService auditLogService)
    {
        EntityFrameworkMappingRuleEditorStore store = new(dbContext);
        return new MappingRuleEditorService(store, new FilterEvaluator(), new MessageRewriter(), auditLogService);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CopyCatDbContext(options);
    }

    private static StoredMessage CreateMessage(
        Guid sourceChannelId,
        long telegramMessageId,
        DateTimeOffset messageDate,
        string text)
    {
        return new StoredMessage
        {
            SessionId = Guid.NewGuid(),
            SourceChannelId = sourceChannelId,
            TelegramMessageId = telegramMessageId,
            MessageDate = messageDate,
            Text = text,
            NormalizedText = text.ToLowerInvariant(),
        };
    }

    private static async Task<ChannelMapping> SeedMappingAsync(CopyCatDbContext dbContext)
    {
        TelegramChannel sourceChannel = new()
        {
            SessionId = Guid.NewGuid(),
            Title = "Source",
            TelegramChannelId = 101,
            AccessHash = "201",
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
        TelegramChannel targetChannel = new()
        {
            SessionId = Guid.NewGuid(),
            Title = "Target",
            TelegramChannelId = 102,
            AccessHash = "202",
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
        ChannelMapping mapping = new() { SourceChannel = sourceChannel, TargetChannel = targetChannel, };

        await dbContext.AddRangeAsync(sourceChannel, targetChannel, mapping);
        await dbContext.SaveChangesAsync();
        return mapping;
    }

    private sealed class RecordingAuditLogService : IAuditLogService
    {
        public List<string> Actions { get; } = [];

        public Task<IReadOnlyList<AuditLogItem>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AuditLogItem>>([]);
        }

        public Task WriteAsync(
            string action,
            string entityType,
            Guid? entityId,
            object? before,
            object? after,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
