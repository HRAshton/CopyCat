using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkRewriteSetStore(CopyCatDbContext dbContext) : IRewriteSetStore
{
    public async Task<IReadOnlyList<RewriteSet>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RewriteSets
            .Include(x => x.Versions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RewriteSet> GetAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RewriteSets
            .Include(x => x.Versions)
            .FirstAsync(x => x.Id == rewriteSetId, cancellationToken);
    }

    public RewriteSet Create()
    {
        RewriteSet rewriteSet = new();
        dbContext.RewriteSets.Add(rewriteSet);
        return rewriteSet;
    }

    public async Task<int> GetNextVersionNumberAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RewriteVersions
            .Where(x => x.RewriteSetId == rewriteSetId)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) is { } maxVersion
            ? maxVersion + 1
            : 1;
    }

    public void AddVersion(RewriteVersion version)
    {
        dbContext.RewriteVersions.Add(version);
    }

    public async Task<RewriteVersion?> GetLatestPublishedVersionAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RewriteVersions
            .OrderByDescending(x => x.Status == RewriteVersionStatus.Published)
            .ThenByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChannelMappings.AnyAsync(
            x => x.ActiveRewriteSetId == rewriteSetId,
            cancellationToken);
    }

    public void RemoveVersions(IEnumerable<RewriteVersion> versions)
    {
        dbContext.RewriteVersions.RemoveRange(versions);
    }

    public void Remove(RewriteSet rewriteSet)
    {
        dbContext.RewriteSets.Remove(rewriteSet);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
