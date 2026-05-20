using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkFilterSetStore(CopyCatDbContext dbContext) : IFilterSetStore
{
    public async Task<IReadOnlyList<FilterSet>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FilterSets
            .Include(x => x.Versions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FilterSet> GetAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FilterSets
            .Include(x => x.Versions)
            .FirstAsync(x => x.Id == filterSetId, cancellationToken);
    }

    public FilterSet Create()
    {
        FilterSet filterSet = new();
        dbContext.FilterSets.Add(filterSet);
        return filterSet;
    }

    public async Task<int> GetNextVersionNumberAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FilterVersions
            .Where(x => x.FilterSetId == filterSetId)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) is { } maxVersion
            ? maxVersion + 1
            : 1;
    }

    public void AddVersion(FilterVersion version)
    {
        dbContext.FilterVersions.Add(version);
    }

    public async Task<IReadOnlyList<StoredMessage>> GetRecentMessagesAsync(
        Guid? channelId,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StoredMessage> query = dbContext.Messages
            .Include(x => x.Attachments)
            .Include(x => x.Links)
            .AsQueryable();
        if (channelId.HasValue)
        {
            query = query.Where(x => x.SourceChannelId == channelId.Value);
        }

        return await query
            .OrderByDescending(x => x.MessageDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings.AnyAsync(
            x => x.ActiveFilterSetId == filterSetId,
            cancellationToken);
    }

    public void RemoveVersions(IEnumerable<FilterVersion> versions)
    {
        dbContext.FilterVersions.RemoveRange(versions);
    }

    public void Remove(FilterSet filterSet)
    {
        dbContext.FilterSets.Remove(filterSet);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
