using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

/// <summary>
/// EF Core implementation of <see cref="IMappingRuleEditorStore"/>.
/// All query logic previously embedded in <c>MappingRuleEditorService</c> lives here so
/// that the service itself depends only on Application-layer abstractions.
/// </summary>
internal sealed class EntityFrameworkMappingRuleEditorStore(CopyCatDbContext dbContext) : IMappingRuleEditorStore
{
    /// <inheritdoc />
    public Task<ChannelMapping> GetMappingWithFilterSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings
            .Include(x => x.ActiveFilterSet)
            .ThenInclude(x => x!.Versions)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ChannelMapping> GetMappingWithChannelsAndFilterSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .Include(x => x.TargetChannel)
            .Include(x => x.ActiveFilterSet)
            .ThenInclude(x => x!.Versions)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ChannelMapping> GetMappingWithRewriteSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings
            .Include(x => x.ActiveRewriteSet)
            .ThenInclude(x => x!.Versions)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ChannelMapping> GetMappingWithChannelsAndRewriteSetAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings
            .Include(x => x.SourceChannel)
            .Include(x => x.TargetChannel)
            .Include(x => x.ActiveRewriteSet)
            .ThenInclude(x => x!.Versions)
            .FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ChannelMapping> GetMappingAsync(
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings.FirstAsync(x => x.Id == mappingId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredMessage>> GetRecentSourceMessagesAsync(
        Guid sourceChannelId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .Include(x => x.Attachments)
            .Include(x => x.Links)
            .Where(x => x.SourceChannelId == sourceChannelId)
            .OrderByDescending(x => x.MessageDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void AddFilterSet(FilterSet filterSet)
    {
        dbContext.FilterSets.Add(filterSet);
    }

    /// <inheritdoc />
    public void AddFilterVersion(FilterVersion version)
    {
        dbContext.FilterVersions.Add(version);
    }

    /// <inheritdoc />
    public Task<bool> IsFilterSetReferencedAsync(Guid filterSetId, CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings.AnyAsync(x => x.ActiveFilterSetId == filterSetId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FilterSet?> FindFilterSetWithVersionsAsync(
        Guid filterSetId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FilterSets
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == filterSetId, cancellationToken);
    }

    /// <inheritdoc />
    public void RemoveFilterSet(FilterSet filterSet)
    {
        dbContext.FilterSets.Remove(filterSet);
    }

    /// <inheritdoc />
    public void RemoveFilterVersions(IReadOnlyList<FilterVersion> versions)
    {
        dbContext.FilterVersions.RemoveRange(versions);
    }

    /// <inheritdoc />
    public void AddRewriteSet(RewriteSet rewriteSet)
    {
        dbContext.RewriteSets.Add(rewriteSet);
    }

    /// <inheritdoc />
    public void AddRewriteVersion(RewriteVersion version)
    {
        dbContext.RewriteVersions.Add(version);
    }

    /// <inheritdoc />
    public Task<bool> IsRewriteSetReferencedAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        return dbContext.ChannelMappings.AnyAsync(x => x.ActiveRewriteSetId == rewriteSetId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RewriteSet?> FindRewriteSetWithVersionsAsync(
        Guid rewriteSetId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.RewriteSets
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == rewriteSetId, cancellationToken);
    }

    /// <inheritdoc />
    public void RemoveRewriteSet(RewriteSet rewriteSet)
    {
        dbContext.RewriteSets.Remove(rewriteSet);
    }

    /// <inheritdoc />
    public void RemoveRewriteVersions(IReadOnlyList<RewriteVersion> versions)
    {
        dbContext.RewriteVersions.RemoveRange(versions);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
