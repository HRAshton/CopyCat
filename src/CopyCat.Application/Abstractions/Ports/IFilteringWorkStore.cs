using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides data access for filtering worker batches.
/// </summary>
public interface IFilteringWorkStore
{
    /// <summary>
    /// Loads enabled mappings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All enabled channel mappings that should be considered by the filtering worker.</returns>
    Task<IReadOnlyList<ChannelMapping>> GetEnabledMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads unrouted messages for a specific mapping.
    /// </summary>
    /// <param name="sourceChannelId">The source channel whose messages should be inspected.</param>
    /// <param name="mappingId">The mapping that still needs routing decisions.</param>
    /// <param name="take">The maximum number of pending messages to return.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The oldest pending stored messages for the mapping.</returns>
    Task<IReadOnlyList<StoredMessage>> GetPendingMessagesAsync(
        Guid sourceChannelId,
        Guid mappingId,
        int take,
        CancellationToken cancellationToken = default);
}
