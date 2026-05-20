using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkFilteringWorkStore(CopyCatDbContext dbContext) : IFilteringWorkStore
{
    public async Task<IReadOnlyList<ChannelMapping>> GetEnabledMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredMessage>> GetPendingMessagesAsync(
        Guid sourceChannelId,
        Guid mappingId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .Include(x => x.Attachments)
            .Include(x => x.Links)
            .Where(x => x.SourceChannelId == sourceChannelId
                        && !dbContext.MessageDecisions.Any(d => d.MessageId == x.Id && d.MappingId == mappingId))
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
