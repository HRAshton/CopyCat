using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Options;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;

namespace CopyCat.Application.Tests;

public sealed class ApplicationMessageFilteringServiceTests
{
    [Fact]
    public async Task ProcessBatchAsync_RequestsConfiguredBatchSize_AndRoutesMessagesAsync()
    {
        ChannelMapping firstMapping = new() { Id = Guid.NewGuid(), SourceChannelId = Guid.NewGuid() };
        ChannelMapping secondMapping = new() { Id = Guid.NewGuid(), SourceChannelId = Guid.NewGuid() };
        StoredMessage firstMessage = new() { Id = Guid.NewGuid() };
        StoredMessage secondMessage = new() { Id = Guid.NewGuid() };
        StoredMessage thirdMessage = new() { Id = Guid.NewGuid() };
        StubFilteringWorkStore store = new(
            [firstMapping, secondMapping],
            new Dictionary<Guid, IReadOnlyList<StoredMessage>>
            {
                [firstMapping.Id] = [firstMessage, secondMessage],
                [secondMapping.Id] = [thirdMessage],
            });
        StubMessageRoutingService routingService = new();
        ApplicationMessageFilteringService sut = new(
            store,
            routingService,
            Microsoft.Extensions.Options.Options.Create(new ApplicationWorkerOptions { FilteringBatchSize = 7 }));

        await sut.ProcessBatchAsync();

        Assert.Equal(
            [
                (firstMapping.SourceChannelId, firstMapping.Id, 7),
                (secondMapping.SourceChannelId, secondMapping.Id, 7),
            ],
            store.PendingMessageRequests);
        Assert.Equal(
            [
                (firstMessage.Id, firstMapping.Id),
                (secondMessage.Id, firstMapping.Id),
                (thirdMessage.Id, secondMapping.Id),
            ],
            routingService.RouteCalls);
    }

    private sealed class StubFilteringWorkStore(
        IReadOnlyList<ChannelMapping> mappings,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredMessage>> pendingMessages) : IFilteringWorkStore
    {
        public List<(Guid SourceChannelId, Guid MappingId, int Take)> PendingMessageRequests { get; } = [];

        public Task<IReadOnlyList<ChannelMapping>> GetEnabledMappingsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(mappings);
        }

        public Task<IReadOnlyList<StoredMessage>> GetPendingMessagesAsync(
            Guid sourceChannelId,
            Guid mappingId,
            int take,
            CancellationToken cancellationToken = default)
        {
            PendingMessageRequests.Add((sourceChannelId, mappingId, take));
            return Task.FromResult(
                pendingMessages.TryGetValue(mappingId, out IReadOnlyList<StoredMessage>? messages)
                    ? messages
                    : (IReadOnlyList<StoredMessage>)[]);
        }
    }

    private sealed class StubMessageRoutingService : IMessageRoutingService
    {
        public List<(Guid MessageId, Guid MappingId)> RouteCalls { get; } = [];

        public Task RouteMessageAsync(
            StoredMessage message,
            ChannelMapping mapping,
            CancellationToken cancellationToken = default)
        {
            RouteCalls.Add((message.Id, mapping.Id));
            return Task.CompletedTask;
        }
    }
}
