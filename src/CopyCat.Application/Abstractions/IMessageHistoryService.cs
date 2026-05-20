using CopyCat.Application.Models;
using CopyCat.Domain.Messages;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Retrieves stored message history for display and debugging.
/// </summary>
public interface IMessageHistoryService
{
    /// <summary>
    /// Returns the most recent stored messages, optionally filtered by channel.
    /// </summary>
    /// <param name="channelId">If provided, limits results to the specified channel.</param>
    /// <param name="take">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of recent message history items.</returns>
    Task<IReadOnlyList<MessageHistoryItem>> GetRecentMessagesAsync(
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the normalized content of a stored message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized message, or <c>null</c> if not found.</returns>
    Task<NormalizedTelegramMessage?> GetNormalizedMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}
