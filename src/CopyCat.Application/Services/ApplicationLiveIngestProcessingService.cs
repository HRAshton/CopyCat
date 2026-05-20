using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Options;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates live-ingest polling batches.
/// </summary>
internal sealed class ApplicationLiveIngestProcessingService(
    ILiveIngestWorkStore liveIngestWorkStore,
    ITelegramGateway telegramGateway,
    IMessageRoutingService messageRoutingService,
    IOptions<ApplicationWorkerOptions> options,
    ILogger<ApplicationLiveIngestProcessingService> logger) : ILiveIngestProcessingService
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Per-source failures must be persisted into sync state while the rest of the batch continues.")]
    public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChannelMapping> liveMappings = await liveIngestWorkStore.GetLiveMappingsAsync(cancellationToken);
        foreach (IGrouping<(Guid SourceChannelId, Guid SessionId), ChannelMapping> sourceGroup in liveMappings
                     .GroupBy(x => (x.SourceChannelId, x.SourceChannel.SessionId)))
        {
            TelegramChannel sourceChannel = sourceGroup.First().SourceChannel;
            TelegramSession session =
                await liveIngestWorkStore.GetSessionAsync(sourceChannel.SessionId, cancellationToken);
            ChannelSyncState syncState = await liveIngestWorkStore.GetOrCreateSyncStateAsync(
                sourceChannel.SessionId,
                sourceChannel.Id,
                cancellationToken);

            IReadOnlyList<StoredMessage> recentMessages;
            try
            {
                recentMessages = await FetchRecentUnseenMessagesAsync(
                    session,
                    sourceChannel,
                    syncState.LastSeenMessageId,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Live ingest failed for channel {ChannelId}.", sourceChannel.Id);
                syncState.SyncStatus = ChannelSyncStatus.Failed;
                syncState.LastError = exception.Message;
                syncState.LastSyncAt = DateTimeOffset.UtcNow;
                await liveIngestWorkStore.SaveChangesAsync(cancellationToken);
                continue;
            }

            foreach (StoredMessage recentMessage in recentMessages.OrderBy(x => x.TelegramMessageId))
            {
                StoredMessage storedMessage = await liveIngestWorkStore.GetOrStoreMessageAsync(
                    recentMessage,
                    cancellationToken);
                foreach (ChannelMapping mapping in sourceGroup)
                {
                    await messageRoutingService.RouteMessageAsync(storedMessage, mapping, cancellationToken);
                }

                syncState.LastSeenMessageId = recentMessage.TelegramMessageId;
            }

            syncState.SyncStatus = ChannelSyncStatus.Live;
            syncState.LastError = null;
            syncState.LastSyncAt = DateTimeOffset.UtcNow;
            await liveIngestWorkStore.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<StoredMessage>> FetchRecentUnseenMessagesAsync(
        TelegramSession session,
        TelegramChannel sourceChannel,
        long? lastSeenMessageId,
        CancellationToken cancellationToken)
    {
        int maxCatchUpPages = (options.Value.LiveIngestBatchSize / 100) + 1;
        int pageSize = options.Value.LiveIngestBatchSize;
        List<StoredMessage> collectedMessages = [];
        long? beforeTelegramMessageId = null;

        for (int pageNumber = 0; pageNumber < maxCatchUpPages; pageNumber++)
        {
            IReadOnlyList<StoredMessage> page = await telegramGateway.BackfillMessagesAsync(
                session,
                sourceChannel,
                pageSize,
                beforeTelegramMessageId,
                cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            List<StoredMessage> orderedPage = page.OrderByDescending(x => x.TelegramMessageId).ToList();
            collectedMessages.AddRange(orderedPage);
            long oldestMessageId = orderedPage.Min(x => x.TelegramMessageId);
            if (lastSeenMessageId.HasValue && oldestMessageId <= lastSeenMessageId.Value)
            {
                break;
            }

            beforeTelegramMessageId = oldestMessageId;
        }

        IEnumerable<StoredMessage> unseenMessages = lastSeenMessageId.HasValue
            ? collectedMessages.Where(x => x.TelegramMessageId > lastSeenMessageId.Value)
            : collectedMessages;

        return unseenMessages
            .GroupBy(x => x.TelegramMessageId)
            .Select(group => group.OrderByDescending(x => x.TelegramMessageId).First())
            .OrderBy(x => x.TelegramMessageId)
            .ToList();
    }
}
