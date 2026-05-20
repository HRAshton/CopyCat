using CopyCat.Application.Models;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Manages Telegram channels discovered for the configured sessions.
/// </summary>
public interface IChannelManagementService
{
    /// <summary>
    /// Returns a summary of all known channels across all sessions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of channel summaries.</returns>
    Task<IReadOnlyList<ChannelSummary>> GetChannelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a channel-discovery operation for every enabled session.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DiscoverChannelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks or un-marks a channel as a forwarding source.
    /// </summary>
    /// <param name="channelId">The channel identifier.</param>
    /// <param name="isSource">Whether the channel should be a source.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSourceStateAsync(Guid channelId, bool isSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks or un-marks a channel as a forwarding target.
    /// </summary>
    /// <param name="channelId">The channel identifier.</param>
    /// <param name="isTarget">Whether the channel should be a target.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetTargetStateAsync(Guid channelId, bool isTarget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a <c>CreateTargetChannel</c> operation for the given source.
    /// </summary>
    /// <param name="sourceChannelId">The source channel identifier.</param>
    /// <param name="title">The title for the new target channel.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateTargetForSourceAsync(Guid sourceChannelId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes a channel and its sync state.
    /// </summary>
    /// <param name="channelId">The channel identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteChannelAsync(Guid channelId, CancellationToken cancellationToken = default);
}
