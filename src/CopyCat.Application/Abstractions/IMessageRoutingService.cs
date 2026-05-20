using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Processes a stored source message for a specific mapping by applying filters, rewrites, and queueing forwarding work.
/// </summary>
public interface IMessageRoutingService
{
    /// <summary>
    /// Applies the configured mapping pipeline to the specified stored message.
    /// </summary>
    /// <param name="message">The stored source message.</param>
    /// <param name="mapping">The mapping to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when routing is persisted.</returns>
    Task RouteMessageAsync(
        StoredMessage message,
        ChannelMapping mapping,
        CancellationToken cancellationToken = default);
}
