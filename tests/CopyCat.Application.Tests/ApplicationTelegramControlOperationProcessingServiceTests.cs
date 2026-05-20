using System.Text.Json;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Models;
using CopyCat.Application.Options;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Tests;

public sealed class ApplicationTelegramControlOperationProcessingServiceTests
{
    [Fact]
    public async Task ProcessNextAsync_WhenNoPendingOperation_ReturnsFalseAsync()
    {
        StubControlOperationWorkStore store = new();
        StubTelegramGateway gateway = new();
        ApplicationTelegramControlOperationProcessingService sut = CreateService(store, gateway);

        bool processed = await sut.ProcessNextAsync();

        Assert.False(processed);
        Assert.NotNull(store.ResetStuckBeforeUtc);
        Assert.Equal(0, store.SaveCallCount);
    }

    [Fact]
    public async Task ProcessNextAsync_DiscoverChannels_CompletesOperationAndPersistsResultsAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.DiscoverChannels,
            SessionId = session.Id,
        };
        IReadOnlyList<TelegramChannel> discoveredChannels =
        [
            new TelegramChannel { Title = "A", SessionId = session.Id, TelegramChannelId = 1 },
            new TelegramChannel { Title = "B", SessionId = session.Id, TelegramChannelId = 2 },
        ];
        StubControlOperationWorkStore store = new() { NextOperation = operation, Session = session, };
        StubTelegramGateway gateway = new() { DiscoveredChannels = discoveredChannels };
        ApplicationTelegramControlOperationProcessingService sut = CreateService(store, gateway);

        bool processed = await sut.ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Succeeded, operation.Status);
        Assert.Equal(1, operation.Attempts);
        Assert.Equal(2, store.SaveCallCount);
        Assert.Same(session, gateway.LastDiscoverSession);
        Assert.Equal(discoveredChannels, store.UpsertedChannels);
        using JsonDocument result = JsonDocument.Parse(operation.ResultJson!);
        Assert.Equal(2, result.RootElement.GetProperty("discovered").GetInt32());
    }

    [Fact]
    public async Task ProcessNextAsync_RunBackfill_UpdatesSyncStateWithNewestMessageAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 10,
            Title = "Source",
        };
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.RunBackfill,
            MappingId = mapping.Id,
            PayloadJson = JsonSerializer.Serialize(new RunBackfillPayload(25)),
        };
        IReadOnlyList<StoredMessage> backfilledMessages =
        [
            new() { TelegramMessageId = 9, SourceChannelId = sourceChannel.Id },
            new() { TelegramMessageId = 21, SourceChannelId = sourceChannel.Id },
            new() { TelegramMessageId = 14, SourceChannelId = sourceChannel.Id },
        ];
        StubControlOperationWorkStore store = new()
        {
            NextOperation = operation,
            Session = session,
            Mapping = mapping,
            InsertedMessages = 2,
        };
        StubTelegramGateway gateway = new() { BackfilledMessages = backfilledMessages };
        ApplicationTelegramControlOperationProcessingService sut = CreateService(store, gateway);

        bool processed = await sut.ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Succeeded, operation.Status);
        Assert.Equal((session.Id, sourceChannel.Id, 21L), store.LastBackfillSyncUpdate);
        Assert.Equal((session.Id, sourceChannel.Id, 25), gateway.LastBackfillRequest);
        using JsonDocument result = JsonDocument.Parse(operation.ResultJson!);
        Assert.Equal(25, result.RootElement.GetProperty("requested").GetInt32());
        Assert.Equal(2, result.RootElement.GetProperty("inserted").GetInt32());
    }

    [Fact]
    public async Task ProcessNextAsync_CreateTargetChannel_CompletesWithPersistedTargetAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 55,
            Title = "Source",
        };
        TelegramChannel createdTarget = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 77,
            Title = "Created",
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.CreateTargetChannel,
            SourceChannelId = sourceChannel.Id,
            PayloadJson = JsonSerializer.Serialize(new CreateTargetChannelPayload("Created")),
        };
        StubControlOperationWorkStore store = new()
        {
            NextOperation = operation,
            Session = session,
            Channel = sourceChannel,
            UpsertedTarget = createdTarget,
        };
        StubTelegramGateway gateway = new() { CreatedTargetChannel = createdTarget };
        ApplicationTelegramControlOperationProcessingService sut = CreateService(store, gateway);

        bool processed = await sut.ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Succeeded, operation.Status);
        Assert.Equal((session.Id, "Created"), gateway.LastCreateTargetRequest);
        using JsonDocument result = JsonDocument.Parse(operation.ResultJson!);
        Assert.Equal(createdTarget.Id, result.RootElement.GetProperty("targetChannelId").GetGuid());
        Assert.Equal("Created", result.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ProcessNextAsync_WhenOperationFails_RecordsRetryAsync()
    {
        TelegramSession disconnectedSession = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Pending };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.DiscoverChannels,
            SessionId = disconnectedSession.Id,
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation, Session = disconnectedSession, };
        ApplicationTelegramControlOperationProcessingService sut = CreateService(store, new StubTelegramGateway());

        bool processed = await sut.ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Equal(1, operation.Attempts);
        Assert.NotNull(operation.NextRetryAt);
        Assert.Contains("not connected", operation.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, store.SaveCallCount);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenDiscoverChannelsSessionIsDisabled_RecordsRetryAsync()
    {
        TelegramSession session = new()
        {
            Id = Guid.NewGuid(),
            Status = TelegramSessionStatus.Connected,
            IsEnabled = false,
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.DiscoverChannels,
            SessionId = session.Id,
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation, Session = session };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("not connected", operation.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenOperationTypeIsUnsupported_RecordsRetryAsync()
    {
        TelegramControlOperation operation = new()
        {
            OperationType = (TelegramControlOperationType)999,
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("Unsupported control operation type", operation.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenDiscoverChannelsSessionIdIsMissing_RecordsRetryAsync()
    {
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.DiscoverChannels,
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("requires SessionId", operation.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenCreateTargetChannelSourceChannelIdIsMissing_RecordsRetryAsync()
    {
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.CreateTargetChannel,
            PayloadJson = JsonSerializer.Serialize(new CreateTargetChannelPayload("Created")),
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("requires SourceChannelId", operation.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenCreateTargetChannelPayloadIsMissing_RecordsRetryAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 56,
            Title = "Source",
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.CreateTargetChannel,
            SourceChannelId = sourceChannel.Id,
        };
        StubControlOperationWorkStore store = new()
        {
            NextOperation = operation,
            Session = session,
            Channel = sourceChannel,
        };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("JSON token", operation.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenRunBackfillMappingIdIsMissing_RecordsRetryAsync()
    {
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.RunBackfill,
            PayloadJson = JsonSerializer.Serialize(new RunBackfillPayload(10)),
        };
        StubControlOperationWorkStore store = new() { NextOperation = operation };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("requires MappingId", operation.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenRunBackfillPayloadIsMissing_RecordsRetryAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 11,
            Title = "Source",
        };
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.RunBackfill,
            MappingId = mapping.Id,
        };
        StubControlOperationWorkStore store = new()
        {
            NextOperation = operation,
            Session = session,
            Mapping = mapping,
        };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Contains("JSON token", operation.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessNextAsync_RunBackfill_WithNoMessages_StoresNullLastBackfilledIdAsync()
    {
        TelegramSession session = new() { Id = Guid.NewGuid(), Status = TelegramSessionStatus.Connected };
        TelegramChannel sourceChannel = new()
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TelegramChannelId = 12,
            Title = "Source",
        };
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
        };
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.RunBackfill,
            MappingId = mapping.Id,
            PayloadJson = JsonSerializer.Serialize(new RunBackfillPayload(5)),
        };
        StubControlOperationWorkStore store = new()
        {
            NextOperation = operation,
            Session = session,
            Mapping = mapping,
        };

        bool processed = await CreateService(store, new StubTelegramGateway()).ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(TelegramControlOperationStatus.Succeeded, operation.Status);
        Assert.Equal((session.Id, sourceChannel.Id, null), store.LastBackfillSyncUpdate);
    }

    private static ApplicationTelegramControlOperationProcessingService CreateService(
        ITelegramControlOperationWorkStore store,
        ITelegramGateway gateway)
    {
        return new ApplicationTelegramControlOperationProcessingService(
            store,
            gateway,
            Microsoft.Extensions.Options.Options.Create(
                new ApplicationWorkerOptions
                {
                    ControlOperationRetryDelay = TimeSpan.FromSeconds(30),
                    ControlOperationStuckThreshold = TimeSpan.FromMinutes(3),
                }));
    }

    private sealed class StubControlOperationWorkStore : ITelegramControlOperationWorkStore
    {
        public TelegramControlOperation? NextOperation { get; init; }

        public TelegramSession Session { get; init; } = new();

        public TelegramChannel Channel { get; init; } = new();

        public ChannelMapping Mapping { get; init; } = new();

        public TelegramChannel UpsertedTarget { get; init; } = new();

        public IReadOnlyList<TelegramChannel>? UpsertedChannels { get; private set; }

        public int SaveCallCount { get; private set; }

        public DateTimeOffset? ResetStuckBeforeUtc { get; private set; }

        public int InsertedMessages { get; init; }

        public (Guid SessionId, Guid ChannelId, long? LastBackfilledMessageId)? LastBackfillSyncUpdate
        {
            get;
            private set;
        }

        public Task<TelegramControlOperation?> GetNextPendingAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextOperation);
        }

        public Task ResetStuckOperationsAsync(
            DateTimeOffset stuckBeforeUtc,
            CancellationToken cancellationToken = default)
        {
            ResetStuckBeforeUtc = stuckBeforeUtc;
            return Task.CompletedTask;
        }

        public Task<TelegramSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(Session.Id, sessionId);
            return Task.FromResult(Session);
        }

        public Task<TelegramChannel> GetChannelAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(Channel.Id, channelId);
            return Task.FromResult(Channel);
        }

        public Task<ChannelMapping> GetMappingWithSourceChannelAsync(
            Guid mappingId,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(Mapping.Id, mappingId);
            return Task.FromResult(Mapping);
        }

        public Task UpsertDiscoveredChannelsAsync(
            Guid sessionId,
            IReadOnlyList<TelegramChannel> channels,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(Session.Id, sessionId);
            UpsertedChannels = channels;
            return Task.CompletedTask;
        }

        public Task<TelegramChannel> UpsertTargetChannelAsync(
            Guid sessionId,
            TelegramChannel target,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(Session.Id, sessionId);
            return Task.FromResult(UpsertedTarget);
        }

        public Task<int> InsertMessagesIfMissingAsync(
            IReadOnlyList<StoredMessage> messages,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InsertedMessages);
        }

        public Task UpdateBackfillSyncStateAsync(
            Guid sessionId,
            Guid channelId,
            long? lastBackfilledMessageId,
            CancellationToken cancellationToken = default)
        {
            LastBackfillSyncUpdate = (sessionId, channelId, lastBackfilledMessageId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTelegramGateway : ITelegramGateway
    {
        public IReadOnlyList<TelegramChannel> DiscoveredChannels { get; init; } = [];

        public IReadOnlyList<StoredMessage> BackfilledMessages { get; init; } = [];

        public TelegramSession? LastDiscoverSession { get; private set; }

        public (Guid SessionId, Guid ChannelId, int Take)? LastBackfillRequest { get; private set; }

        public TelegramChannel CreatedTargetChannel { get; init; } = new();

        public (Guid SessionId, string Title)? LastCreateTargetRequest { get; private set; }

        public Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(
            TelegramSession session,
            CancellationToken cancellationToken = default)
        {
            LastDiscoverSession = session;
            return Task.FromResult(DiscoveredChannels);
        }

        public Task<TelegramChannel> CreateTargetChannelAsync(
            TelegramSession session,
            string title,
            CancellationToken cancellationToken = default)
        {
            LastCreateTargetRequest = (session.Id, title);
            return Task.FromResult(CreatedTargetChannel);
        }

        public Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(
            TelegramSession session,
            TelegramChannel sourceChannel,
            int take,
            long? beforeTelegramMessageId = null,
            CancellationToken cancellationToken = default)
        {
            LastBackfillRequest = (session.Id, sourceChannel.Id, take);
            return Task.FromResult(BackfilledMessages);
        }

        public Task StartLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartQrLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SubmitCodeAsync(TelegramSession session, string code, CancellationToken cancellationToken = default)
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
}
