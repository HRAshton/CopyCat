using CopyCat.Application.Abstractions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Messages;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Services;

/// <summary>
/// Provides recent stored message history and normalized message inspection helpers.
/// </summary>
public sealed class MessageHistoryService(CopyCatDbContext dbContext) : IMessageHistoryService
{
    /// <summary>
    /// Loads recent stored messages for the optional source channel filter.
    /// </summary>
    /// <param name="channelId">An optional source channel filter; when omitted, messages from all channels are returned.</param>
    /// <param name="take">The maximum number of recent messages to return.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested recent message history items.</returns>
    public async Task<IReadOnlyList<MessageHistoryItem>> GetRecentMessagesAsync(
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StoredMessage> query = dbContext.Messages.Include(x => x.Attachments).AsQueryable();
        if (channelId.HasValue)
        {
            query = query.Where(x => x.SourceChannelId == channelId.Value);
        }

        return await query
            .Join(
                dbContext.TelegramChannels,
                message => message.SourceChannelId,
                channel => channel.Id,
                (message, channel) => new { message, channel })
            .OrderByDescending(x => x.message.MessageDate)
            .ThenByDescending(x => x.message.TelegramMessageId)
            .Take(take)
            .Select(x => new MessageHistoryItem(
                x.message.Id,
                x.message.MessageDate,
                x.channel.Title,
                x.message.TelegramMessageId,
                x.message.Text ?? x.message.Caption,
                string.Join(", ", x.message.Attachments.Select(a => a.AttachmentType)),
                "Stored",
                null))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a stored message and projects it into the normalized rules-engine shape.
    /// </summary>
    /// <param name="messageId">The identifier of the stored message to normalize.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The normalized message, or <see langword="null"/> when the stored message does not exist.</returns>
    public async Task<NormalizedTelegramMessage?> GetNormalizedMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        StoredMessage? message = await dbContext.Messages.Include(x => x.Attachments).Include(x => x.Links)
            .FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        return message is null ? null : NormalizedTelegramMessage.FromEntity(message);
    }
}
