using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_AggregatesCurrentOperationalCounts()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession enabledHealthy = new()
        {
            Id = Guid.NewGuid(),
            Name = "Healthy",
            IsEnabled = true,
            Status = TelegramSessionStatus.Connected,
        };
        TelegramSession enabledFaulted = new()
        {
            Id = Guid.NewGuid(),
            Name = "Faulted",
            IsEnabled = true,
            Status = TelegramSessionStatus.Faulted,
            LastError = "failed",
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        TelegramSession disabledFaulted = new()
        {
            Id = Guid.NewGuid(),
            Name = "Disabled",
            IsEnabled = false,
            Status = TelegramSessionStatus.Faulted,
            LastError = "older failed",
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        TelegramChannel source = CreateChannel(enabledHealthy.Id, "Source", isSource: true);
        TelegramChannel target = CreateChannel(enabledHealthy.Id, "Target", isTarget: true);
        ChannelMapping enabledMapping = new()
        {
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
            IsEnabled = true,
        };
        ChannelMapping disabledMapping = new()
        {
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
            IsEnabled = false,
        };
        ForwardingJob pending = new() { MappingId = enabledMapping.Id, MessageId = Guid.NewGuid(), Status = ForwardingJobStatus.Pending };
        ForwardingJob processing = new() { MappingId = enabledMapping.Id, MessageId = Guid.NewGuid(), Status = ForwardingJobStatus.Processing };
        ForwardingJob failedTransient = new() { MappingId = enabledMapping.Id, MessageId = Guid.NewGuid(), Status = ForwardingJobStatus.FailedTransient };
        ForwardingJob failedPermanent = new() { MappingId = enabledMapping.Id, MessageId = Guid.NewGuid(), Status = ForwardingJobStatus.FailedPermanent };

        await dbContext.AddRangeAsync(
            enabledHealthy,
            enabledFaulted,
            disabledFaulted,
            source,
            target,
            enabledMapping,
            disabledMapping,
            pending,
            processing,
            failedTransient,
            failedPermanent);
        await dbContext.SaveChangesAsync();

        DashboardService service = new(dbContext);

        Application.Models.DashboardSnapshot snapshot = await service.GetSnapshotAsync();

        Assert.Equal(2, snapshot.EnabledSessions);
        Assert.Equal(2, snapshot.UnhealthySessions);
        Assert.Equal(1, snapshot.ActiveSourceChannels);
        Assert.Equal(1, snapshot.ActiveMappings);
        Assert.Equal(2, snapshot.PendingJobs);
        Assert.Equal(2, snapshot.FailedJobs);
        Assert.Equal(
            ["Faulted: failed", "Disabled: older failed"],
            snapshot.LatestErrors.ToArray());
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CopyCatDbContext(options);
    }

    private static TelegramChannel CreateChannel(Guid sessionId, string title, bool isSource = false, bool isTarget = false)
    {
        return new TelegramChannel
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TelegramChannelId = Random.Shared.NextInt64(1, int.MaxValue),
            Title = title,
            ChannelType = TelegramChannelType.BroadcastChannel,
            IsSource = isSource,
            IsTarget = isTarget,
        };
    }
}
