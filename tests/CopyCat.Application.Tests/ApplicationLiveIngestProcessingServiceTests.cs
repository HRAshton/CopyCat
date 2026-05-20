using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Options;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

using Microsoft.Extensions.Logging.Abstractions;

namespace CopyCat.Application.Tests;

public sealed class ApplicationLiveIngestProcessingServiceTests
{
    [Fact]
    public async Task ProcessBatchAsync_RoutesRecentUnseenMessages_AndUpdatesSyncStateAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 111,
            Title = "Source",
        };
        ChannelMapping firstMapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        ChannelMapping secondMapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        ChannelSyncState syncState =
            new() { SessionId = session.Id, ChannelId = sourceChannel.Id, LastSeenMessageId = 100 };
        StoredMessage message101 = CreateMessage(session.Id, sourceChannel.Id, 101);
        StoredMessage message103 = CreateMessage(session.Id, sourceChannel.Id, 103);
        StoredMessage duplicateMessage103 = CreateMessage(session.Id, sourceChannel.Id, 103);
        StoredMessage oldMessage100 = CreateMessage(session.Id, sourceChannel.Id, 100);
        StubLiveIngestWorkStore store = new([firstMapping, secondMapping], session, syncState);
        StubTelegramGateway gateway = new(
            [message103, message101],
            new Dictionary<long, IReadOnlyList<StoredMessage>> { [101] = [duplicateMessage103, oldMessage100], });
        StubMessageRoutingService routingService = new();
        ApplicationLiveIngestProcessingService sut = new(
            store,
            gateway,
            routingService,
            Microsoft.Extensions.Options.Options.Create(new ApplicationWorkerOptions { LiveIngestBatchSize = 100 }),
            NullLogger<ApplicationLiveIngestProcessingService>.Instance);

        await sut.ProcessBatchAsync();

        Assert.Equal(
            [(session.Id, sourceChannel.Id, null), (session.Id, sourceChannel.Id, 101L)],
            gateway.BackfillCalls);
        Assert.Equal([101L, 103L], store.StoredCandidates.Select(x => x.TelegramMessageId).ToArray());
        Assert.Equal(
            [
                (101L, firstMapping.Id),
                (101L, secondMapping.Id),
                (103L, firstMapping.Id),
                (103L, secondMapping.Id),
            ],
            routingService.RouteCalls);
        Assert.Equal(ChannelSyncStatus.Live, syncState.SyncStatus);
        Assert.Equal(103, syncState.LastSeenMessageId);
        Assert.Null(syncState.LastError);
        Assert.NotNull(syncState.LastSyncAt);
        Assert.Equal(1, store.SaveCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenGatewayThrows_MarksSyncStateFailedAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 222,
            Title = "Source",
        };
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        ChannelSyncState syncState = new() { SessionId = session.Id, ChannelId = sourceChannel.Id };
        StubLiveIngestWorkStore store = new([mapping], session, syncState);
        StubTelegramGateway gateway = new(shouldThrow: true);
        StubMessageRoutingService routingService = new();
        ApplicationLiveIngestProcessingService sut = new(
            store,
            gateway,
            routingService,
            Microsoft.Extensions.Options.Options.Create(new ApplicationWorkerOptions { LiveIngestBatchSize = 100 }),
            NullLogger<ApplicationLiveIngestProcessingService>.Instance);

        await sut.ProcessBatchAsync();

        Assert.Equal(ChannelSyncStatus.Failed, syncState.SyncStatus);
        Assert.Equal("Gateway exploded", syncState.LastError);
        Assert.NotNull(syncState.LastSyncAt);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Empty(store.StoredCandidates);
        Assert.Empty(routingService.RouteCalls);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenNoLastSeenExists_RoutesCollectedMessagesAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 444,
            Title = "Source",
        };
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        ChannelSyncState syncState = new() { SessionId = session.Id, ChannelId = sourceChannel.Id };
        StoredMessage message7 = CreateMessage(session.Id, sourceChannel.Id, 7);
        StoredMessage message8 = CreateMessage(session.Id, sourceChannel.Id, 8);
        StubLiveIngestWorkStore store = new([mapping], session, syncState);
        StubTelegramGateway gateway = new([message8, message7]);
        StubMessageRoutingService routingService = new();
        ApplicationLiveIngestProcessingService sut = new(
            store,
            gateway,
            routingService,
            Microsoft.Extensions.Options.Options.Create(new ApplicationWorkerOptions { LiveIngestBatchSize = 100 }),
            NullLogger<ApplicationLiveIngestProcessingService>.Instance);

        await sut.ProcessBatchAsync();

        Assert.Equal([7L, 8L], store.StoredCandidates.Select(x => x.TelegramMessageId).ToArray());
        Assert.Equal([(7L, mapping.Id), (8L, mapping.Id)], routingService.RouteCalls);
        Assert.Equal(8, syncState.LastSeenMessageId);
    }

    private static StoredMessage CreateMessage(Guid sessionId, Guid sourceChannelId, long telegramMessageId)
    {
        return new StoredMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SourceChannelId = sourceChannelId,
            TelegramMessageId = telegramMessageId,
            MessageDate = DateTimeOffset.UtcNow,
            Text = $"message {telegramMessageId}",
            NormalizedText = $"message {telegramMessageId}",
        };
    }

    private sealed class StubLiveIngestWorkStore(
        IReadOnlyList<ChannelMapping> mappings,
        TelegramSession session,
        ChannelSyncState syncState) : ILiveIngestWorkStore
    {
        public List<StoredMessage> StoredCandidates { get; } = [];

        public int SaveCallCount { get; private set; }

        public Task<IReadOnlyList<ChannelMapping>> GetLiveMappingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(mappings);
        }

        public Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(session.Id, sessionId);
            return Task.FromResult(session);
        }

        public Task<ChannelSyncState> GetOrCreateSyncStateAsync(
            Guid sessionId,
            Guid channelId,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(syncState.SessionId, sessionId);
            Assert.Equal(syncState.ChannelId, channelId);
            return Task.FromResult(syncState);
        }

        public Task<StoredMessage> GetOrStoreMessageAsync(
            StoredMessage candidate,
            CancellationToken cancellationToken = default)
        {
            StoredCandidates.Add(candidate);
            return Task.FromResult(candidate);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTelegramGateway(
        IReadOnlyList<StoredMessage>? firstPage = null,
        IReadOnlyDictionary<long, IReadOnlyList<StoredMessage>>? pages = null,
        bool shouldThrow = false)
        : ITelegramGateway
    {
        private readonly IReadOnlyDictionary<long, IReadOnlyList<StoredMessage>> pages = pages
            ?? new Dictionary<long, IReadOnlyList<StoredMessage>>();

        public List<(Guid SessionId, Guid SourceChannelId, long? BeforeTelegramMessageId)> BackfillCalls { get; } = [];

        private IReadOnlyList<StoredMessage> FirstPage { get; } = firstPage ?? [];

        public Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(
            TelegramSession session,
            TelegramChannel sourceChannel,
            int take,
            long? beforeTelegramMessageId = null,
            CancellationToken cancellationToken = default)
        {
            BackfillCalls.Add((session.Id, sourceChannel.Id, beforeTelegramMessageId));
            if (shouldThrow)
            {
                throw new InvalidOperationException("Gateway exploded");
            }

            if (!beforeTelegramMessageId.HasValue)
            {
                return Task.FromResult(FirstPage);
            }

            return Task.FromResult(
                pages.TryGetValue(beforeTelegramMessageId.Value, out IReadOnlyList<StoredMessage>? page)
                    ? page
                    : (IReadOnlyList<StoredMessage>)[]);
        }

        public Task StartLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartQrLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SubmitCodeAsync(
            TelegramSession session,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ResendCodeAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SubmitPasswordAsync(
            TelegramSession session,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(
            TelegramSession session,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TelegramChannel>>([]);
        }

        public Task<TelegramChannel> CreateTargetChannelAsync(
            TelegramSession session,
            string title,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TelegramChannel());
        }

        public Task<long?> ExecuteForwardingAsync(
            TelegramSession session,
            TelegramChannel sourceChannel,
            TelegramChannel targetChannel,
            StoredMessage message,
            ForwardingMode forwardingMode,
            Domain.Rewriting.RewriteResult? rewriteResult,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<long?>(null);
        }
    }

    private sealed class StubMessageRoutingService : IMessageRoutingService
    {
        public List<(long TelegramMessageId, Guid MappingId)> RouteCalls { get; } = [];

        public Task RouteMessageAsync(
            StoredMessage message,
            ChannelMapping mapping,
            CancellationToken cancellationToken = default)
        {
            RouteCalls.Add((message.TelegramMessageId, mapping.Id));
            return Task.CompletedTask;
        }
    }
}
