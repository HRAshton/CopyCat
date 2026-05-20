using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Services;

/// <summary>
/// Coordinates rewrite set administration use cases.
/// </summary>
internal sealed class ApplicationRewriteManagementService(
    IRewriteSetStore rewriteSetStore,
    IAuditLogService auditLogService) : IRewriteManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RewriteSetSummary>> GetRewriteSetsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RewriteSet> sets = await rewriteSetStore.ListAsync(cancellationToken);
        return sets.Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<RewriteSetSummary> SaveDraftAsync(
        RewriteSetEditorModel model,
        CancellationToken cancellationToken = default)
    {
        RewriteSet rewriteSet = model.RewriteSetId.HasValue
            ? await rewriteSetStore.GetAsync(model.RewriteSetId.Value, cancellationToken)
            : rewriteSetStore.Create();
        int nextVersionNumber = model.RewriteSetId.HasValue
            ? await rewriteSetStore.GetNextVersionNumberAsync(rewriteSet.Id, cancellationToken)
            : 1;

        rewriteSet.Name = model.Name;
        rewriteSet.Description = model.Description;
        RewriteVersion newVersion = new()
        {
            RewriteSetId = rewriteSet.Id,
            VersionNumber = nextVersionNumber,
            Status = RewriteVersionStatus.Draft,
            Rules = model.Rules,
        };

        rewriteSetStore.AddVersion(newVersion);
        await rewriteSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "rewrite.saved",
            nameof(RewriteSet),
            rewriteSet.Id,
            null,
            new
            {
                rewriteSet.Id,
                rewriteSet.Name,
                rewriteSet.Description,
                VersionId = newVersion.Id,
                newVersion.VersionNumber,
                newVersion.Status,
            },
            cancellationToken);

        RewriteSet saved = await rewriteSetStore.GetAsync(rewriteSet.Id, cancellationToken);
        return ToSummary(saved);
    }

    /// <inheritdoc />
    public async Task PublishLatestAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        RewriteSet rewriteSet = await rewriteSetStore.GetAsync(rewriteSetId, cancellationToken);
        foreach (RewriteVersion version in rewriteSet.Versions)
        {
            version.Status = RewriteVersionStatus.Archived;
        }

        RewriteVersion latest = rewriteSet.Versions.OrderByDescending(x => x.VersionNumber).First();
        latest.Status = RewriteVersionStatus.Published;
        latest.PublishedAt = DateTimeOffset.UtcNow;
        await rewriteSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "rewrite.published",
            nameof(RewriteSet),
            rewriteSet.Id,
            null,
            new
            {
                rewriteSet.Id,
                LatestVersionId = latest.Id,
                latest.VersionNumber,
                latest.Status,
                latest.PublishedAt,
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRewriteSetAsync(Guid rewriteSetId, CancellationToken cancellationToken = default)
    {
        bool inUse = await rewriteSetStore.IsInUseAsync(rewriteSetId, cancellationToken);
        if (inUse)
        {
            throw new InvalidDomainOperationException("This rewrite set is still attached to one or more mappings.");
        }

        RewriteSet rewriteSet = await rewriteSetStore.GetAsync(rewriteSetId, cancellationToken);
        rewriteSetStore.RemoveVersions(rewriteSet.Versions);
        rewriteSetStore.Remove(rewriteSet);
        await rewriteSetStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "rewrite.deleted",
            nameof(RewriteSet),
            rewriteSetId,
            null,
            new { Id = rewriteSetId },
            cancellationToken);
    }

    private static RewriteSetSummary ToSummary(RewriteSet set)
    {
        RewriteVersion? latest = set.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        return new RewriteSetSummary(
            set.Id,
            set.Name,
            set.Description,
            set.Versions.Count,
            latest?.Rules);
    }
}
