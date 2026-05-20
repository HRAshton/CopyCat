using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class ForwardingJobServiceTests
{
    [Fact]
    public async Task GetJobsAsync_ProjectsRecentHistory_AndRetryJobAsync_ResetsStatus()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Id = Guid.NewGuid(), Name = "Session" };
        TelegramChannel source = CreateChannel(session.Id, "Source", 100);
        TelegramChannel target = CreateChannel(session.Id, "Target", 200);
        ChannelMapping mapping = new()
        {
            Id = Guid.NewGuid(),
            SourceChannelId = source.Id,
            SourceChannel = source,
            TargetChannelId = target.Id,
            TargetChannel = target,
        };
        ForwardingJob older = new()
        {
            Id = Guid.NewGuid(),
            MappingId = mapping.Id,
            MessageId = Guid.NewGuid(),
            Status = ForwardingJobStatus.Succeeded,
            ForwardingMode = ForwardingMode.CopyAsIs,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };
        ForwardingJob failed = new()
        {
            Id = Guid.NewGuid(),
            MappingId = mapping.Id,
            MessageId = Guid.NewGuid(),
            Status = ForwardingJobStatus.FailedPermanent,
            ForwardingMode = ForwardingMode.CopyWithRewriting,
            Attempts = 3,
            LastError = "boom",
            NextRetryAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await dbContext.AddRangeAsync(session, source, target, mapping, older, failed);
        await dbContext.SaveChangesAsync();

        ForwardingJobService service = new(dbContext);

        IReadOnlyList<Application.Models.ForwardingJobItem> jobs = await service.GetJobsAsync();

        Assert.Equal([failed.Id, older.Id], jobs.Select(x => x.Id).ToArray());
        Assert.Equal("Source", jobs[0].SourceChannelTitle);
        Assert.Equal("Target", jobs[0].TargetChannelTitle);
        Assert.Equal("boom", jobs[0].LastError);
        Assert.Equal(3, jobs[0].Attempts);

        await service.RetryJobAsync(failed.Id);

        ForwardingJob reloaded = await dbContext.ForwardingJobs.SingleAsync(x => x.Id == failed.Id);
        Assert.Equal(ForwardingJobStatus.Pending, reloaded.Status);
        Assert.NotNull(reloaded.NextRetryAt);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CopyCatDbContext(options);
    }

    private static TelegramChannel CreateChannel(Guid sessionId, string title, long telegramChannelId)
    {
        return new TelegramChannel
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TelegramChannelId = telegramChannelId,
            Title = title,
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
    }
}
