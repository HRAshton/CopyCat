using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Options;
using CopyCat.Domain.Entities;

using Microsoft.Extensions.Options;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates filtering worker batches.
/// </summary>
internal sealed class ApplicationMessageFilteringService(
    IFilteringWorkStore filteringWorkStore,
    IMessageRoutingService messageRoutingService,
    IOptions<ApplicationWorkerOptions> options) : IMessageFilteringService
{
    /// <inheritdoc />
    public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChannelMapping> mappings = await filteringWorkStore.GetEnabledMappingsAsync(cancellationToken);
        foreach (ChannelMapping mapping in mappings)
        {
            IReadOnlyList<StoredMessage> pendingMessages = await filteringWorkStore.GetPendingMessagesAsync(
                mapping.SourceChannelId,
                mapping.Id,
                options.Value.FilteringBatchSize,
                cancellationToken);
            foreach (StoredMessage message in pendingMessages)
            {
                await messageRoutingService.RouteMessageAsync(message, mapping, cancellationToken);
            }
        }
    }
}
