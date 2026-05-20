using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Stores;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class EntityFrameworkStoreTests
{
    [Fact]
    public async Task ForwardingWorkStore_ReturnsReadyJobs_AndHydratesExecutionContextAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 10);
        TelegramChannel target = CreateChannel(session.Id, "Target", 20);
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
        };
        RewriteSet rewriteSet = new();
        RewriteVersion rewriteVersion = new()
        {
            Id = Guid.NewGuid(),
            RewriteSet = rewriteSet,
            VersionNumber = 1,
            Status = RewriteVersionStatus.Published,
        };
        StoredMessage message = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SourceChannelId = source.Id,
            TelegramMessageId = 100,
            MessageDate = DateTimeOffset.UtcNow,
            Attachments = [new MessageAttachment { AttachmentType = AttachmentType.Photo }],
            Links = [new MessageLink { Url = "https://example.test", LinkType = "Url" }],
        };
        ForwardingJob readyPending = new()
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            MappingId = mapping.Id,
            RewriteVersionId = rewriteVersion.Id,
            Status = ForwardingJobStatus.Pending,
        };
        ForwardingJob readyRetry = new()
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            MappingId = mapping.Id,
            Status = ForwardingJobStatus.FailedTransient,
            NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
        };
        ForwardingJob notReady = new()
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            MappingId = mapping.Id,
            Status = ForwardingJobStatus.FailedTransient,
            NextRetryAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        await dbContext.AddRangeAsync(
            session,
            source,
            target,
            mapping,
            rewriteSet,
            rewriteVersion,
            message,
            readyPending,
            readyRetry,
            notReady);
        await dbContext.SaveChangesAsync();

        EntityFrameworkForwardingWorkStore store = new(dbContext);

        IReadOnlyList<ForwardingJob> readyJobs = await store.GetReadyJobsAsync(10);
        ForwardingExecutionContext context = await store.GetExecutionContextAsync(readyPending.Id);

        Assert.Equal([readyPending.Id, readyRetry.Id], readyJobs.Select(x => x.Id).ToArray());
        Assert.Equal(readyPending.Id, context.Job.Id);
        Assert.Equal(mapping.Id, context.Mapping.Id);
        Assert.Equal(source.Id, context.SourceChannel.Id);
        Assert.Equal(target.Id, context.TargetChannel.Id);
        Assert.Equal(session.Id, context.Session.Id);
        Assert.Single(context.Message.Attachments);
        Assert.Single(context.Message.Links);
        Assert.Equal(rewriteVersion.Id, context.RewriteVersion!.Id);
    }

    [Fact]
    public async Task SessionStore_ListsAlphabetically_AndFindsDependentsAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession beta = new() { Id = Guid.NewGuid(), Name = "Beta" };
        TelegramSession alpha = new() { Id = Guid.NewGuid(), Name = "Alpha" };
        TelegramSession orphan = new() { Id = Guid.NewGuid(), Name = "Orphan" };
        TelegramChannel channel = CreateChannel(alpha.Id, "Channel", 11);
        StoredMessage message = new()
        {
            SessionId = alpha.Id,
            SourceChannelId = channel.Id,
            TelegramMessageId = 1,
            MessageDate = DateTimeOffset.UtcNow,
        };

        await dbContext.AddRangeAsync(beta, alpha, orphan, channel, message);
        await dbContext.SaveChangesAsync();

        EntityFrameworkSessionStore store = new(dbContext);

        IReadOnlyList<TelegramSession> sessions = await store.ListAsync();

        Assert.Equal(["Alpha", "Beta", "Orphan"], sessions.Select(x => x.Name).ToArray());
        Assert.True(await store.HasDependentsAsync(alpha.Id));
        Assert.False(await store.HasDependentsAsync(orphan.Id));

        TelegramSession added = new() { Id = Guid.NewGuid(), Name = "Zulu" };
        await store.AddAsync(added);
        await store.SaveChangesAsync();
        store.Remove(added);
        await store.SaveChangesAsync();

        Assert.DoesNotContain(dbContext.TelegramSessions, x => x.Id == added.Id);
    }

    [Fact]
    public async Task MappingStore_LoadsRelations_ChecksExists_AndResolvesSourceSessionAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 31);
        TelegramChannel target = CreateChannel(session.Id, "Target", 32);
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
        };

        await dbContext.AddRangeAsync(session, source, target, mapping);
        await dbContext.SaveChangesAsync();

        EntityFrameworkMappingStore store = new(dbContext);

        IReadOnlyList<ChannelMapping> mappings = await store.ListAsync();
        ChannelMapping loaded = await store.GetAsync(mapping.Id);

        Assert.Single(mappings);
        Assert.Equal("Source", loaded.SourceChannel.Title);
        Assert.Equal("Target", loaded.TargetChannel.Title);
        Assert.True(await store.ExistsAsync(source.Id, target.Id, excludeMappingId: null));
        Assert.False(await store.ExistsAsync(source.Id, target.Id, excludeMappingId: mapping.Id));
        Assert.Equal(session.Id, (await store.GetSourceSessionAsync(mapping.Id)).Id);

        ChannelMapping added = new() { Id = Guid.NewGuid(), SourceChannelId = source.Id, TargetChannelId = target.Id };
        store.Add(added);
        await store.SaveChangesAsync();
        store.Remove(added);
        await store.SaveChangesAsync();
        Assert.DoesNotContain(dbContext.ChannelMappings, x => x.Id == added.Id);
    }

    [Fact]
    public async Task ControlOperationScheduler_DetectsPendingWork_AndEnqueuesAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        Guid sessionId = Guid.NewGuid();
        TelegramControlOperation pending = new()
        {
            SessionId = sessionId,
            OperationType = TelegramControlOperationType.DiscoverChannels,
            Status = TelegramControlOperationStatus.Pending,
        };
        TelegramControlOperation completed = new()
        {
            SessionId = sessionId,
            OperationType = TelegramControlOperationType.RunBackfill,
            Status = TelegramControlOperationStatus.Succeeded,
        };

        await dbContext.AddRangeAsync(pending, completed);
        await dbContext.SaveChangesAsync();

        EntityFrameworkTelegramControlOperationScheduler scheduler = new(dbContext);

        Assert.True(await scheduler.HasPendingAsync(sessionId, TelegramControlOperationType.DiscoverChannels));
        Assert.False(await scheduler.HasPendingAsync(sessionId, TelegramControlOperationType.RunBackfill));

        TelegramControlOperation enqueued = new()
        {
            SessionId = sessionId,
            OperationType = TelegramControlOperationType.CreateTargetChannel,
            Status = TelegramControlOperationStatus.Pending,
        };
        await scheduler.EnqueueAsync(enqueued);

        Assert.Contains(dbContext.TelegramControlOperations, x => x.Id == enqueued.Id);
    }

    [Fact]
    public async Task ChannelStore_ListsByDiscovery_GetConnectedSessions_AndDetectsDependentsAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession connected = new() { Id = Guid.NewGuid(), Name = "Connected", Status = TelegramSessionStatus.Connected, IsEnabled = true };
        TelegramSession disabled = new() { Id = Guid.NewGuid(), Name = "Disabled", Status = TelegramSessionStatus.Connected, IsEnabled = false };
        TelegramSession pending = new() { Id = Guid.NewGuid(), Name = "Pending", Status = TelegramSessionStatus.Pending, IsEnabled = true };
        TelegramChannel older = CreateChannel(connected.Id, "Older", 41);
        older.DiscoveredAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        TelegramChannel newer = CreateChannel(connected.Id, "Newer", 42);
        newer.DiscoveredAt = DateTimeOffset.UtcNow;
        TelegramChannel orphan = CreateChannel(disabled.Id, "Orphan", 43);
        ChannelMapping mapping = new() { SourceChannelId = newer.Id, TargetChannelId = orphan.Id };
        StoredMessage message = new() { SessionId = connected.Id, SourceChannelId = newer.Id, TelegramMessageId = 9, MessageDate = DateTimeOffset.UtcNow };

        await dbContext.AddRangeAsync(connected, disabled, pending, older, newer, orphan, mapping, message);
        await dbContext.SaveChangesAsync();

        EntityFrameworkChannelStore store = new(dbContext);

        IReadOnlyList<TelegramChannel> channels = await store.ListAsync();
        IReadOnlyList<TelegramSession> connectedSessions = await store.GetConnectedSessionsAsync();

        Assert.Equal(["Orphan", "Newer", "Older"], channels.Select(x => x.Title).ToArray());
        Assert.Equal(connected.Id, (await store.GetOwningSessionAsync(newer.Id)).Id);
        Assert.Equal([connected.Id], connectedSessions.Select(x => x.Id).ToArray());
        Assert.True(await store.HasDependentsAsync(newer.Id));
        Assert.False(await store.HasDependentsAsync(older.Id));

        store.Remove(older);
        await store.SaveChangesAsync();
        Assert.DoesNotContain(dbContext.TelegramChannels, x => x.Id == older.Id);
    }

    [Fact]
    public async Task FilterSetStore_ListsVersions_GetsRecentMessages_AndTracksUsageAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 51);
        TelegramChannel target = CreateChannel(session.Id, "Target", 52);
        FilterSet filterSet = new() { Name = "Filter A" };
        FilterVersion version = new() { FilterSet = filterSet, VersionNumber = 2, Status = FilterVersionStatus.Published };
        ChannelMapping mapping = new() { SourceChannelId = source.Id, TargetChannelId = target.Id, ActiveFilterSetId = filterSet.Id };
        StoredMessage older = new() { SessionId = session.Id, SourceChannelId = source.Id, TelegramMessageId = 1, MessageDate = DateTimeOffset.UtcNow.AddMinutes(-5) };
        StoredMessage newer = new()
        {
            SessionId = session.Id,
            SourceChannelId = source.Id,
            TelegramMessageId = 2,
            MessageDate = DateTimeOffset.UtcNow,
            Attachments = [new MessageAttachment { AttachmentType = AttachmentType.Photo }],
            Links = [new MessageLink { Url = "https://example.test", LinkType = "Url" }],
        };

        await dbContext.AddRangeAsync(session, source, target, filterSet, version, mapping, older, newer);
        await dbContext.SaveChangesAsync();

        EntityFrameworkFilterSetStore store = new(dbContext);

        IReadOnlyList<FilterSet> sets = await store.ListAsync();
        FilterSet loaded = await store.GetAsync(filterSet.Id);
        IReadOnlyList<StoredMessage> filteredMessages = await store.GetRecentMessagesAsync(source.Id, 2);

        Assert.Single(sets);
        Assert.Single(loaded.Versions);
        Assert.Equal(3, await store.GetNextVersionNumberAsync(filterSet.Id));
        Assert.True(await store.IsInUseAsync(filterSet.Id));
        Assert.Equal([2L, 1L], filteredMessages.Select(x => x.TelegramMessageId).ToArray());
        Assert.Single(filteredMessages[0].Attachments);
        Assert.Single(filteredMessages[0].Links);

        FilterSet created = store.Create();
        store.AddVersion(new FilterVersion { FilterSet = created, VersionNumber = 1, Status = FilterVersionStatus.Draft });
        await store.SaveChangesAsync();
        store.RemoveVersions(created.Versions);
        store.Remove(created);
        await store.SaveChangesAsync();
        Assert.DoesNotContain(dbContext.FilterSets, x => x.Id == created.Id);
    }

    [Fact]
    public async Task RewriteSetStore_ListsVersions_ResolvesLatestPublished_AndTracksUsageAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 61);
        TelegramChannel target = CreateChannel(session.Id, "Target", 62);
        RewriteSet rewriteSet = new() { Name = "Rewrite A" };
        RewriteVersion archived = new() { RewriteSet = rewriteSet, VersionNumber = 1, Status = RewriteVersionStatus.Archived };
        RewriteVersion published = new() { RewriteSet = rewriteSet, VersionNumber = 2, Status = RewriteVersionStatus.Published };
        ChannelMapping mapping = new() { SourceChannelId = source.Id, TargetChannelId = target.Id, ActiveRewriteSetId = rewriteSet.Id };

        await dbContext.AddRangeAsync(session, source, target, rewriteSet, archived, published, mapping);
        await dbContext.SaveChangesAsync();

        EntityFrameworkRewriteSetStore store = new(dbContext);

        IReadOnlyList<RewriteSet> sets = await store.ListAsync();
        RewriteSet loaded = await store.GetAsync(rewriteSet.Id);
        RewriteVersion latest = Assert.IsType<RewriteVersion>(await store.GetLatestPublishedVersionAsync());

        Assert.Single(sets);
        Assert.Equal(2, loaded.Versions.Count);
        Assert.Equal(3, await store.GetNextVersionNumberAsync(rewriteSet.Id));
        Assert.Equal(published.Id, latest.Id);
        Assert.True(await store.IsInUseAsync(rewriteSet.Id));

        RewriteSet created = store.Create();
        store.AddVersion(new RewriteVersion { RewriteSet = created, VersionNumber = 1, Status = RewriteVersionStatus.Draft });
        await store.SaveChangesAsync();
        store.RemoveVersions(created.Versions);
        store.Remove(created);
        await store.SaveChangesAsync();
        Assert.DoesNotContain(dbContext.RewriteSets, x => x.Id == created.Id);
    }

    [Fact]
    public async Task LiveIngestWorkStore_ListsMappings_CreatesSyncState_AndStoresMessagesAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel liveSource = CreateChannel(session.Id, "Live", 71);
        TelegramChannel nonLiveSource = CreateChannel(session.Id, "NonLive", 72);
        ChannelMapping liveMapping = new()
        {
            SourceChannelId = liveSource.Id,
            SourceChannel = liveSource,
            TargetChannelId = liveSource.Id,
            TargetChannel = liveSource,
            IsEnabled = true,
            LiveForwardingEnabled = true,
        };
        ChannelMapping disabledLiveMapping = new()
        {
            SourceChannelId = nonLiveSource.Id,
            SourceChannel = nonLiveSource,
            TargetChannelId = nonLiveSource.Id,
            TargetChannel = nonLiveSource,
            IsEnabled = false,
            LiveForwardingEnabled = true,
        };

        await dbContext.AddRangeAsync(session, liveSource, nonLiveSource, liveMapping, disabledLiveMapping);
        await dbContext.SaveChangesAsync();

        EntityFrameworkLiveIngestWorkStore store = new(dbContext);

        IReadOnlyList<ChannelMapping> liveMappings = await store.GetLiveMappingsAsync();
        TelegramSession loadedSession = await store.GetSessionAsync(session.Id);
        ChannelSyncState createdState = await store.GetOrCreateSyncStateAsync(session.Id, liveSource.Id);
        await store.SaveChangesAsync();
        ChannelSyncState reloadedState = await store.GetOrCreateSyncStateAsync(session.Id, liveSource.Id);

        StoredMessage candidate = new()
        {
            SessionId = session.Id,
            SourceChannelId = liveSource.Id,
            TelegramMessageId = 700,
            MessageDate = DateTimeOffset.UtcNow,
            Text = "hello",
            Attachments = [new MessageAttachment { AttachmentType = AttachmentType.Photo }],
            Links = [new MessageLink { Url = "https://example.test", LinkType = "Url" }],
        };
        StoredMessage stored = await store.GetOrStoreMessageAsync(candidate);
        StoredMessage existing = await store.GetOrStoreMessageAsync(
            new StoredMessage
            {
                SessionId = session.Id,
                SourceChannelId = liveSource.Id,
                TelegramMessageId = 700,
                MessageDate = DateTimeOffset.UtcNow,
            });

        Assert.Equal([liveMapping.Id], liveMappings.Select(x => x.Id).ToArray());
        Assert.Equal(session.Id, loadedSession.Id);
        Assert.Equal(createdState.Id, reloadedState.Id);
        Assert.Equal(stored.Id, existing.Id);
        Assert.Single(existing.Attachments);
        Assert.Single(existing.Links);
    }

    [Fact]
    public async Task TelegramControlOperationWorkStore_ResetsStuck_UpsertsChannels_AndTracksBackfillAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session", Status = TelegramSessionStatus.Connected };
        TelegramChannel source = CreateChannel(session.Id, "Source", 81);
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = source.Id,
            TargetChannel = source,
        };
        TelegramControlOperation stuck = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OperationType = TelegramControlOperationType.DiscoverChannels,
            Status = TelegramControlOperationStatus.Processing,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
        };
        TelegramControlOperation ready = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OperationType = TelegramControlOperationType.RunBackfill,
            Status = TelegramControlOperationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        TelegramControlOperation futureRetry = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OperationType = TelegramControlOperationType.CreateTargetChannel,
            Status = TelegramControlOperationStatus.Pending,
            NextRetryAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        TelegramChannel discoveredExisting = CreateChannel(session.Id, "Old title", 82);
        StoredMessage existingMessage = new()
        {
            SessionId = session.Id,
            SourceChannelId = source.Id,
            TelegramMessageId = 1,
            MessageDate = DateTimeOffset.UtcNow.AddMinutes(-1),
        };

        await dbContext.AddRangeAsync(
            session,
            source,
            mapping,
            stuck,
            ready,
            futureRetry,
            discoveredExisting,
            existingMessage);
        await dbContext.SaveChangesAsync();

        EntityFrameworkTelegramControlOperationWorkStore store = new(dbContext);

        TelegramControlOperation? nextBeforeReset = await store.GetNextPendingAsync();
        await store.ResetStuckOperationsAsync(DateTimeOffset.UtcNow.AddHours(-1));
        TelegramControlOperation? nextAfterReset = await store.GetNextPendingAsync();
        TelegramSession loadedSession = await store.GetSessionAsync(session.Id);
        TelegramChannel loadedChannel = await store.GetChannelAsync(source.Id);
        ChannelMapping loadedMapping = await store.GetMappingWithSourceChannelAsync(mapping.Id);

        await store.UpsertDiscoveredChannelsAsync(
            session.Id,
            [
                new TelegramChannel
                {
                    SessionId = session.Id,
                    TelegramChannelId = discoveredExisting.TelegramChannelId,
                    Title = "Updated title",
                    Username = "updated",
                    ChannelType = TelegramChannelType.Group,
                    CanPost = true,
                },
                new TelegramChannel
                {
                    SessionId = session.Id,
                    TelegramChannelId = 83,
                    Title = "New discovery",
                    ChannelType = TelegramChannelType.BroadcastChannel,
                },
            ]);

        TelegramChannel persistedTarget = await store.UpsertTargetChannelAsync(
            session.Id,
            new TelegramChannel
            {
                TelegramChannelId = 84,
                Title = "Target",
                ChannelType = TelegramChannelType.BroadcastChannel,
            });

        int inserted = await store.InsertMessagesIfMissingAsync(
            [
                new StoredMessage
                {
                    SessionId = session.Id,
                    SourceChannelId = source.Id,
                    TelegramMessageId = 1,
                    MessageDate = DateTimeOffset.UtcNow,
                },
                new StoredMessage
                {
                    SessionId = session.Id,
                    SourceChannelId = source.Id,
                    TelegramMessageId = 2,
                    MessageDate = DateTimeOffset.UtcNow,
                },
            ]);
        await store.UpdateBackfillSyncStateAsync(session.Id, source.Id, 900);
        await store.SaveChangesAsync();

        TelegramControlOperation resetOperation = await dbContext.TelegramControlOperations.SingleAsync(x => x.Id == stuck.Id);
        TelegramChannel updatedExisting = await dbContext.TelegramChannels.SingleAsync(x => x.Id == discoveredExisting.Id);
        TelegramChannel newDiscovered = await dbContext.TelegramChannels.SingleAsync(x => x.TelegramChannelId == 83);
        ChannelSyncState syncState = await dbContext.ChannelSyncStates.SingleAsync(x => x.SessionId == session.Id && x.ChannelId == source.Id);

        Assert.Equal(ready.Id, nextBeforeReset!.Id);
        Assert.Equal(stuck.Id, nextAfterReset!.Id);
        Assert.Equal(TelegramControlOperationStatus.Pending, resetOperation.Status);
        Assert.Null(resetOperation.StartedAt);
        Assert.Equal(session.Id, loadedSession.Id);
        Assert.Equal(source.Id, loadedChannel.Id);
        Assert.Equal(source.Id, loadedMapping.SourceChannel.Id);
        Assert.Equal("Updated title", updatedExisting.Title);
        Assert.Equal("updated", updatedExisting.Username);
        Assert.Equal(TelegramChannelType.Group, updatedExisting.ChannelType);
        Assert.Equal("New discovery", newDiscovered.Title);
        Assert.True(persistedTarget.IsTarget);
        Assert.Equal(1, inserted);
        Assert.Equal(900, syncState.LastBackfilledMessageId);
        Assert.Equal(ChannelSyncStatus.Backfilled, syncState.SyncStatus);
        Assert.Null(syncState.LastError);
    }

    [Fact]
    public async Task FilteringWorkStore_ReturnsEnabledMappings_AndOnlyUndecidedMessagesAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 91);
        TelegramChannel target = CreateChannel(session.Id, "Target", 92);
        ChannelMapping enabledMapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
            IsEnabled = true,
        };
        ChannelMapping disabledMapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
            IsEnabled = false,
        };
        StoredMessage pendingMessage = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SourceChannelId = source.Id,
            TelegramMessageId = 10,
            MessageDate = DateTimeOffset.UtcNow,
            Attachments = [new MessageAttachment { AttachmentType = AttachmentType.Photo }],
            Links = [new MessageLink { Url = "https://example.test/pending", LinkType = "Url" }],
        };
        StoredMessage decidedMessage = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SourceChannelId = source.Id,
            TelegramMessageId = 11,
            MessageDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            Attachments = [new MessageAttachment { AttachmentType = AttachmentType.Document }],
            Links = [new MessageLink { Url = "https://example.test/decided", LinkType = "Url" }],
        };
        MessageDecision decision = new()
        {
            MessageId = decidedMessage.Id,
            MappingId = enabledMapping.Id,
            Decision = DecisionKind.Accepted,
        };

        await dbContext.AddRangeAsync(
            session,
            source,
            target,
            enabledMapping,
            disabledMapping,
            pendingMessage,
            decidedMessage,
            decision);
        await dbContext.SaveChangesAsync();

        EntityFrameworkFilteringWorkStore store = new(dbContext);

        IReadOnlyList<ChannelMapping> mappings = await store.GetEnabledMappingsAsync();
        IReadOnlyList<StoredMessage> messages = await store.GetPendingMessagesAsync(source.Id, enabledMapping.Id, 10);

        Assert.Equal([enabledMapping.Id], mappings.Select(x => x.Id).ToArray());
        StoredMessage onlyMessage = Assert.Single(messages);
        Assert.Equal(pendingMessage.Id, onlyMessage.Id);
        Assert.Single(onlyMessage.Attachments);
        Assert.Single(onlyMessage.Links);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CopyCatDbContext(options);
    }

    private static TelegramChannel CreateChannel(Guid sessionId, string title, long telegramChannelId)
    {
        return new TelegramChannel
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TelegramChannelId = telegramChannelId,
            Title = title,
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
    }
}
