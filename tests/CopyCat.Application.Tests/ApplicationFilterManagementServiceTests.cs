using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;

namespace CopyCat.Application.Tests;

public sealed class ApplicationFilterManagementServiceTests
{
    [Fact]
    public async Task SaveDraftAsync_NewSet_CreatesDraftVersionAndAudits()
    {
        StubFilterSetStore store = new();
        StubAuditLogService audit = new();
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), audit);
        FilterSetEditorModel model = new(
            null,
            "Links only",
            "Filters telegram links",
            new FilterSetDefinition("Links only", MappingDefaultPolicy.Reject, new HasTelegramLinkCondition(true, "telegram")));

        FilterSetSummary summary = await sut.SaveDraftAsync(model);

        FilterVersion version = Assert.Single(store.AddedVersions);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(FilterVersionStatus.Draft, version.Status);
        Assert.Equal("Links only", summary.Name);
        Assert.Equal("filter.saved", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task GetFilterSetsAsync_MapsLatestDefinition_AndHandlesEmptySets()
    {
        FilterSet empty = new() { Name = "Empty", Description = "No versions yet" };
        FilterSet versioned = new()
        {
            Name = "Versioned",
            Description = "Has versions",
            Versions =
            [
                new() { VersionNumber = 1, Status = FilterVersionStatus.Draft, FilterDefinition = new FilterSetDefinition("v1", MappingDefaultPolicy.Reject, null) },
                new() { VersionNumber = 2, Status = FilterVersionStatus.Published, FilterDefinition = new FilterSetDefinition("v2", MappingDefaultPolicy.Allow, null) },
            ],
        };
        StubFilterSetStore store = new(empty) { ListedSets = [empty, versioned] };
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), new StubAuditLogService());

        IReadOnlyList<FilterSetSummary> result = await sut.GetFilterSetsAsync();

        Assert.Equal(2, result.Count);
        Assert.Null(result.Single(x => x.Name == "Empty").LatestDefinition);
        Assert.Equal("v2", result.Single(x => x.Name == "Versioned").LatestDefinition!.Name);
    }

    [Fact]
    public async Task SaveDraftAsync_ExistingSet_AppendsNextVersion()
    {
        FilterSet set = new()
        {
            Name = "Existing",
            Versions = [new() { VersionNumber = 1, Status = FilterVersionStatus.Published }],
        };
        StubFilterSetStore store = new(set);
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), new StubAuditLogService());

        FilterSetSummary summary = await sut.SaveDraftAsync(
            new FilterSetEditorModel(set.Id, "Existing", "Updated", new FilterSetDefinition("Existing", MappingDefaultPolicy.Allow, null)));

        FilterVersion version = Assert.Single(store.AddedVersions);
        Assert.Equal(2, version.VersionNumber);
        Assert.Equal(set.Id, version.FilterSetId);
        Assert.Equal(2, summary.VersionCount);
    }

    [Fact]
    public async Task PublishLatestAsync_ArchivesExistingVersions_AndPublishesLatest()
    {
        FilterSet set = new() { Name = "Rules", Versions = [new() { VersionNumber = 1, Status = FilterVersionStatus.Published }, new() { VersionNumber = 2, Status = FilterVersionStatus.Draft }] };
        StubFilterSetStore store = new(set);
        StubAuditLogService audit = new();
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), audit);

        await sut.PublishLatestAsync(set.Id);

        Assert.All(set.Versions.Where(x => x.VersionNumber == 1), x => Assert.Equal(FilterVersionStatus.Archived, x.Status));
        FilterVersion latest = set.Versions.Single(x => x.VersionNumber == 2);
        Assert.Equal(FilterVersionStatus.Published, latest.Status);
        Assert.NotNull(latest.PublishedAt);
        Assert.Equal("filter.published", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task DebugAsync_UsesLatestPublishedVersion_AndMapsDecisions()
    {
        FilterSet set = new()
        {
            Versions =
            [
                new() { VersionNumber = 1, Status = FilterVersionStatus.Draft, FilterDefinition = new FilterSetDefinition("Draft", MappingDefaultPolicy.Reject, null) },
                new() { VersionNumber = 2, Status = FilterVersionStatus.Published, FilterDefinition = new FilterSetDefinition("Published", MappingDefaultPolicy.Allow, null) },
            ],
        };
        StoredMessage message = new()
        {
            TelegramMessageId = 99,
            MessageDate = DateTimeOffset.UtcNow,
            Text = "Hello world",
        };
        StubFilterSetStore store = new(set) { RecentMessages = [message] };
        StubFilterEvaluator evaluator = new() { NextDecision = new FilterDecision(true, "rule-1", ["ok"], "Accepted by filter.") };
        ApplicationFilterManagementService sut = new(store, evaluator, new StubAuditLogService());

        IReadOnlyList<FilterDebugResult> results = await sut.DebugAsync(set.Id, null, 5);

        FilterDebugResult result = Assert.Single(results);
        Assert.Equal(99, result.TelegramMessageId);
        Assert.True(result.Accepted);
        Assert.Equal("rule-1", result.MatchedRuleId);
        Assert.Equal("Published", evaluator.LastDefinition!.Name);
    }

    [Fact]
    public async Task DeleteFilterSetAsync_WhenInUse_Throws()
    {
        FilterSet set = new();
        StubFilterSetStore store = new(set) { InUse = true };
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.DeleteFilterSetAsync(set.Id));

        Assert.Contains("still attached", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteFilterSetAsync_WhenUnused_RemovesVersionsAndAudits()
    {
        FilterSet set = new()
        {
            Versions = [new FilterVersion()],
        };
        StubFilterSetStore store = new(set);
        StubAuditLogService audit = new();
        ApplicationFilterManagementService sut = new(store, new StubFilterEvaluator(), audit);

        await sut.DeleteFilterSetAsync(set.Id);

        Assert.True(store.Removed);
        Assert.Empty(set.Versions);
        Assert.Equal("filter.deleted", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task DebugAsync_UsesCaptionWhenTextIsMissing()
    {
        FilterSet set = new()
        {
            Versions =
            [
                new() { VersionNumber = 1, Status = FilterVersionStatus.Published, FilterDefinition = new FilterSetDefinition("Published", MappingDefaultPolicy.Allow, null) },
            ],
        };
        StoredMessage message = new()
        {
            TelegramMessageId = 15,
            MessageDate = DateTimeOffset.UtcNow,
            Caption = "caption only",
        };
        StubFilterSetStore store = new(set) { RecentMessages = [message] };
        StubFilterEvaluator evaluator = new() { NextDecision = new FilterDecision(false, null, [], "Rejected") };
        ApplicationFilterManagementService sut = new(store, evaluator, new StubAuditLogService());

        FilterDebugResult result = Assert.Single(await sut.DebugAsync(set.Id, null, 1));

        Assert.Equal("caption only", result.Preview);
        Assert.False(result.Accepted);
    }

    [Fact]
    public async Task DebugAsync_UsesEmptyStringWhenTextAndCaptionAreMissing()
    {
        FilterSet set = new()
        {
            Versions =
            [
                new() { VersionNumber = 1, Status = FilterVersionStatus.Published, FilterDefinition = new FilterSetDefinition("Published", MappingDefaultPolicy.Allow, null) },
            ],
        };
        StoredMessage message = new()
        {
            TelegramMessageId = 16,
            MessageDate = DateTimeOffset.UtcNow,
        };
        StubFilterSetStore store = new(set) { RecentMessages = [message] };
        StubFilterEvaluator evaluator = new() { NextDecision = new FilterDecision(true, null, ["trace"], "Accepted") };
        ApplicationFilterManagementService sut = new(store, evaluator, new StubAuditLogService());

        FilterDebugResult result = Assert.Single(await sut.DebugAsync(set.Id, null, 1));

        Assert.Equal(string.Empty, result.Preview);
        Assert.True(result.Accepted);
    }

    private sealed class StubFilterSetStore : IFilterSetStore
    {
        private readonly FilterSet set;

        public StubFilterSetStore()
        {
            set = new FilterSet();
        }

        public StubFilterSetStore(FilterSet set)
        {
            this.set = set;
        }

        public List<FilterVersion> AddedVersions { get; } = [];

        public IReadOnlyList<FilterSet> ListedSets { get; init; } = [];

        public IReadOnlyList<StoredMessage> RecentMessages { get; init; } = [];

        public bool InUse { get; init; }

        public bool Removed { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<FilterSet>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListedSets);
        }

        public Task<FilterSet> GetAsync(Guid filterSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(set);
        }

        public FilterSet Create()
        {
            return set;
        }

        public Task<int> GetNextVersionNumberAsync(Guid filterSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(set.Versions.Count + 1);
        }

        public void AddVersion(FilterVersion version)
        {
            AddedVersions.Add(version);
            set.Versions.Add(version);
        }

        public Task<IReadOnlyList<StoredMessage>> GetRecentMessagesAsync(Guid? channelId, int take, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecentMessages);
        }

        public Task<bool> IsInUseAsync(Guid filterSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InUse);
        }

        public void RemoveVersions(IEnumerable<FilterVersion> versions)
        {
            set.Versions.Clear();
        }

        public void Remove(FilterSet filterSet)
        {
            Removed = true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubFilterEvaluator : IFilterEvaluator
    {
        public FilterDecision NextDecision { get; init; } = new(true, null, [], "Accepted");

        public FilterSetDefinition? LastDefinition { get; private set; }

        public FilterDecision Evaluate(CopyCat.Domain.Messages.NormalizedTelegramMessage message, FilterSetDefinition filterSet)
        {
            LastDefinition = filterSet;
            return NextDecision;
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
