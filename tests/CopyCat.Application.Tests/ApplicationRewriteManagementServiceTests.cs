using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Tests;

public sealed class ApplicationRewriteManagementServiceTests
{
    [Fact]
    public async Task SaveDraftAsync_NewSet_CreatesVersionAndAudits()
    {
        StubRewriteSetStore store = new();
        StubAuditLogService audit = new();
        ApplicationRewriteManagementService sut = new(store, audit);

        RewriteSetSummary summary = await sut.SaveDraftAsync(
            new RewriteSetEditorModel(null, "Footer", "Adds footer", new RewriteRuleSet([new AppendFooterOperation("Footer")])));

        RewriteVersion version = Assert.Single(store.AddedVersions);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("Footer", summary.Name);
        Assert.Equal("rewrite.saved", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task GetRewriteSetsAsync_MapsLatestRules_AndHandlesEmptySets()
    {
        RewriteSet empty = new() { Name = "Empty", Description = "No versions yet" };
        RewriteSet versioned = new()
        {
            Name = "Versioned",
            Description = "Has versions",
            Versions =
            [
                new() { VersionNumber = 1, Status = RewriteVersionStatus.Draft, Rules = new RewriteRuleSet([new AppendFooterOperation("v1")]) },
                new() { VersionNumber = 2, Status = RewriteVersionStatus.Published, Rules = new RewriteRuleSet([new StripAllTextOperation()]) },
            ],
        };
        StubRewriteSetStore store = new(empty) { ListedSets = [empty, versioned] };
        ApplicationRewriteManagementService sut = new(store, new StubAuditLogService());

        IReadOnlyList<RewriteSetSummary> result = await sut.GetRewriteSetsAsync();

        Assert.Equal(2, result.Count);
        Assert.Null(result.Single(x => x.Name == "Empty").LatestRules);
        Assert.IsType<StripAllTextOperation>(Assert.Single(result.Single(x => x.Name == "Versioned").LatestRules!.EffectiveOperations));
    }

    [Fact]
    public async Task SaveDraftAsync_ExistingSet_AppendsNextVersion()
    {
        RewriteSet set = new()
        {
            Name = "Existing",
            Versions = [new() { VersionNumber = 1, Status = RewriteVersionStatus.Published }],
        };
        StubRewriteSetStore store = new(set);
        ApplicationRewriteManagementService sut = new(store, new StubAuditLogService());

        RewriteSetSummary summary = await sut.SaveDraftAsync(
            new RewriteSetEditorModel(set.Id, "Existing", "Updated", new RewriteRuleSet([new AppendFooterOperation("Footer")])));

        RewriteVersion version = Assert.Single(store.AddedVersions);
        Assert.Equal(2, version.VersionNumber);
        Assert.Equal(set.Id, version.RewriteSetId);
        Assert.Equal(2, summary.VersionCount);
    }

    [Fact]
    public async Task PublishLatestAsync_ArchivesOlderVersions()
    {
        RewriteSet set = new() { Versions = [new() { VersionNumber = 1, Status = RewriteVersionStatus.Published }, new() { VersionNumber = 2, Status = RewriteVersionStatus.Draft }] };
        StubRewriteSetStore store = new(set);
        StubAuditLogService audit = new();
        ApplicationRewriteManagementService sut = new(store, audit);

        await sut.PublishLatestAsync(set.Id);

        Assert.All(set.Versions.Where(x => x.VersionNumber == 1), x => Assert.Equal(RewriteVersionStatus.Archived, x.Status));
        Assert.Equal(RewriteVersionStatus.Published, set.Versions.Single(x => x.VersionNumber == 2).Status);
        Assert.Equal("rewrite.published", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task DeleteRewriteSetAsync_WhenInUse_Throws()
    {
        RewriteSet set = new();
        StubRewriteSetStore store = new(set) { InUse = true };
        ApplicationRewriteManagementService sut = new(store, new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.DeleteRewriteSetAsync(set.Id));

        Assert.Contains("still attached", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteRewriteSetAsync_WhenNotInUse_RemovesVersionsAndAudits()
    {
        RewriteSet set = new() { Versions = [new RewriteVersion()] };
        StubRewriteSetStore store = new(set);
        StubAuditLogService audit = new();
        ApplicationRewriteManagementService sut = new(store, audit);

        await sut.DeleteRewriteSetAsync(set.Id);

        Assert.Empty(set.Versions);
        Assert.True(store.Removed);
        Assert.Equal("rewrite.deleted", Assert.Single(audit.Entries).Action);
    }

    private sealed class StubRewriteSetStore : IRewriteSetStore
    {
        private readonly RewriteSet set;

        public StubRewriteSetStore()
            : this(new RewriteSet())
        {
        }

        public StubRewriteSetStore(RewriteSet set)
        {
            this.set = set;
        }

        public List<RewriteVersion> AddedVersions { get; } = [];

        public IReadOnlyList<RewriteSet> ListedSets { get; init; } = [];

        public bool InUse { get; init; }

        public bool Removed { get; private set; }

        public Task<IReadOnlyList<RewriteSet>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListedSets);
        }

        public Task<RewriteSet> GetAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(set);
        }

        public RewriteSet Create()
        {
            return set;
        }

        public Task<int> GetNextVersionNumberAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(set.Versions.Count + 1);
        }

        public void AddVersion(RewriteVersion version)
        {
            AddedVersions.Add(version);
            set.Versions.Add(version);
        }

        public Task<RewriteVersion?> GetLatestPublishedVersionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<RewriteVersion?>(set.Versions.FirstOrDefault(x => x.Status == RewriteVersionStatus.Published));
        }

        public Task<bool> IsInUseAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InUse);
        }

        public void RemoveVersions(IEnumerable<RewriteVersion> versions)
        {
            set.Versions.Clear();
        }

        public void Remove(RewriteSet rewriteSet)
        {
            Removed = true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
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
