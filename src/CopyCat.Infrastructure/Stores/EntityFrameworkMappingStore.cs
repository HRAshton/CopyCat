using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkMappingStore(CopyCatDbContext dbContext) : IMappingStore
{
    public async Task<IReadOnlyList<ChannelMapping>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .Include(x => x.TargetChannel)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChannelMapping> GetAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .Include(x => x.TargetChannel)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid sourceChannelId,
        Guid targetChannelId,
        Guid? excludeMappingId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings.AnyAsync(
            x => x.SourceChannelId == sourceChannelId
                 && x.TargetChannelId == targetChannelId
                 && (!excludeMappingId.HasValue || x.Id != excludeMappingId.Value),
            cancellationToken);
    }

    public void Add(ChannelMapping mapping)
    {
        dbContext.ChannelMappings.Add(mapping);
    }

    public void Remove(ChannelMapping mapping)
    {
        dbContext.ChannelMappings.Remove(mapping);
    }

    public async Task<TelegramSession> GetSourceSessionAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        Guid sessionId = await dbContext.ChannelMappings
            .Where(x => x.Id == mappingId)
            .Select(x => x.SourceChannel.SessionId)
            .FirstAsync(cancellationToken);

        return await dbContext.TelegramSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
