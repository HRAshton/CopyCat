using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Tests;

public sealed class ChannelServiceTests
{
    [Fact]
    public async Task GetChannelsAsync_OrdersByDiscoveryDateDescending()
    {
        TelegramChannel older = new() { Title = "Older", SessionId = Guid.NewGuid(), DiscoveredAt = DateTimeOffset.UtcNow.AddHours(-1) };
        TelegramChannel newer = new() { Title = "Newer", SessionId = Guid.NewGuid(), DiscoveredAt = DateTimeOffset.UtcNow };
        StubChannelStore store = new([older, newer], []);
        ChannelService sut = new(store, new StubScheduler(), new StubAuditLogService());

        IReadOnlyList<ChannelSummary> result = await sut.GetChannelsAsync();

        Assert.Equal(["Newer", "Older"], result.Select(x => x.Title).ToArray());
    }

    [Fact]
    public async Task DiscoverChannelsAsync_WithoutConnectedSessions_Throws()
    {
        ChannelService sut = new(new StubChannelStore([], []), new StubScheduler(), new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.DiscoverChannelsAsync());

        Assert.Contains("No connected Telegram sessions are available", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DiscoverChannelsAsync_QueuesOnlySessionsWithoutPendingOperation()
    {
        TelegramSession first = new() { Status = TelegramSessionStatus.Connected };
        TelegramSession second = new() { Status = TelegramSessionStatus.Connected };
        StubChannelStore store = new([], [first, second]);
        StubScheduler scheduler = new() { PendingSessions = [first.Id] };
        StubAuditLogService audit = new();
        ChannelService sut = new(store, scheduler, audit);

        await sut.DiscoverChannelsAsync();

        TelegramControlOperation operation = Assert.Single(scheduler.Enqueued);
        Assert.Equal(second.Id, operation.SessionId);
        Assert.Equal(TelegramControlOperationType.DiscoverChannels, operation.OperationType);
        Assert.Equal("channel.discovery-queued", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task SetSourceAndTargetState_UpdateFlagsAndAudit()
    {
        TelegramChannel channel = new() { SessionId = Guid.NewGuid(), Title = "Channel" };
        StubChannelStore store = new([channel], []);
        StubAuditLogService audit = new();
        ChannelService sut = new(store, new StubScheduler(), audit);

        await sut.SetSourceStateAsync(channel.Id, true);
        await sut.SetTargetStateAsync(channel.Id, true);

        Assert.True(channel.IsSource);
        Assert.True(channel.IsTarget);
        Assert.Equal(["channel.source-toggled", "channel.target-toggled"], audit.Entries.Select(x => x.Action).ToArray());
    }

    [Fact]
    public async Task CreateTargetForSourceAsync_ConnectedSession_QueuesTrimmedTitle()
    {
        TelegramChannel source = new() { SessionId = Guid.NewGuid(), Title = "Source" };
        TelegramSession session = new() { Id = source.SessionId, Status = TelegramSessionStatus.Connected };
        StubChannelStore store = new([source], [], session);
        StubScheduler scheduler = new();
        StubAuditLogService audit = new();
        ChannelService sut = new(store, scheduler, audit);

        await sut.CreateTargetForSourceAsync(source.Id, "  New Target  ");

        TelegramControlOperation operation = Assert.Single(scheduler.Enqueued);
        CreateTargetChannelPayload payload = JsonSerializer.Deserialize<CreateTargetChannelPayload>(operation.PayloadJson!)!;
        Assert.Equal(source.Id, operation.SourceChannelId);
        Assert.Equal(TelegramControlOperationType.CreateTargetChannel, operation.OperationType);
        Assert.Equal("New Target", payload.Title);
        Assert.Equal("channel.target-create-queued", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task CreateTargetForSourceAsync_WhenSessionIsNotConnected_Throws()
    {
        TelegramChannel source = new() { SessionId = Guid.NewGuid(), Title = "Source" };
        TelegramSession session = new() { Id = source.SessionId, Status = TelegramSessionStatus.Pending };
        StubChannelStore store = new([source], [], session);
        ChannelService sut = new(store, new StubScheduler(), new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.CreateTargetForSourceAsync(source.Id, "New Target"));

        Assert.Contains("source session is not connected", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteChannelAsync_WithDependents_Throws()
    {
        TelegramChannel channel = new() { SessionId = Guid.NewGuid(), Title = "Channel" };
        StubChannelStore store = new([channel], []) { HasDependents = true };
        ChannelService sut = new(store, new StubScheduler(), new StubAuditLogService());

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.DeleteChannelAsync(channel.Id));

        Assert.Contains("Only unused channels can be deleted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteChannelAsync_WithoutDependents_RemovesChannelAndAudits()
    {
        TelegramChannel channel = new() { SessionId = Guid.NewGuid(), Title = "Channel" };
        StubChannelStore store = new([channel], []);
        StubAuditLogService audit = new();
        ChannelService sut = new(store, new StubScheduler(), audit);

        await sut.DeleteChannelAsync(channel.Id);

        Assert.Same(channel, store.RemovedChannel);
        Assert.Equal("channel.deleted", Assert.Single(audit.Entries).Action);
    }

    private sealed class StubChannelStore(
        IReadOnlyList<TelegramChannel> channels,
        IReadOnlyList<TelegramSession> connectedSessions,
        TelegramSession? owningSession = null) : IChannelStore
    {
        private readonly Dictionary<Guid, TelegramChannel> channelsById = channels.ToDictionary(x => x.Id);

        private readonly IReadOnlyList<TelegramSession> connectedSessions = connectedSessions;

        private readonly TelegramSession? owningSession = owningSession;

        public bool HasDependents { get; init; }

        public TelegramChannel? RemovedChannel { get; private set; }

        public Task<IReadOnlyList<TelegramChannel>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TelegramChannel>>(channelsById.Values.ToList());
        }

        public Task<TelegramChannel> GetAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(channelsById[channelId]);
        }

        public Task<IReadOnlyList<TelegramSession>> GetConnectedSessionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(connectedSessions);
        }

        public Task<TelegramSession> GetOwningSessionAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(owningSession!);
        }

        public Task<bool> HasDependentsAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HasDependents);
        }

        public void Remove(TelegramChannel channel)
        {
            RemovedChannel = channel;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubScheduler : ITelegramControlOperationScheduler
    {
        public HashSet<Guid> PendingSessions { get; init; } = [];

        public List<TelegramControlOperation> Enqueued { get; } = [];

        public Task<bool> HasPendingAsync(Guid sessionId, TelegramControlOperationType operationType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PendingSessions.Contains(sessionId));
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
