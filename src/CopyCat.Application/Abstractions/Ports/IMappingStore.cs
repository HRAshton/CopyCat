using CopyCat.Domain.Entities;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Provides persistence operations for channel mappings.
/// </summary>
public interface IMappingStore
{
    /// <summary>
    /// Loads mappings with source and target channel details.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>All channel mappings with their source and target channels.</returns>
    Task<IReadOnlyList<ChannelMapping>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a mapping by identifier with source and target channel details.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping to load.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The requested mapping with source and target channel details.</returns>
    Task<ChannelMapping> GetAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether another mapping already uses the same source and target channels.
    /// </summary>
    /// <param name="sourceChannelId">The candidate source channel.</param>
    /// <param name="targetChannelId">The candidate target channel.</param>
    /// <param name="excludeMappingId">An optional mapping identifier to exclude during uniqueness checks.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when a conflicting mapping exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(
        Guid sourceChannelId,
        Guid targetChannelId,
        Guid? excludeMappingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new mapping to persistence.
    /// </summary>
    /// <param name="mapping">The mapping entity to begin tracking.</param>
    void Add(ChannelMapping mapping);

    /// <summary>
    /// Removes a mapping from persistence.
    /// </summary>
    /// <param name="mapping">The tracked mapping entity to remove.</param>
    void Remove(ChannelMapping mapping);

    /// <summary>
    /// Loads the connected session that owns the mapping source channel.
    /// </summary>
    /// <param name="mappingId">The identifier of the mapping whose source session should be loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns>The connected session that owns the mapping source channel.</returns>
    Task<TelegramSession> GetSourceSessionAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the pending changes have been committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
