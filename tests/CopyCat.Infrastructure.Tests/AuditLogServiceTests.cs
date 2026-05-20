using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task WriteAsync_PersistsEntries_AndGetRecentAsync_ReturnsNewestFirst()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        AuditLogService service = new(dbContext);
        Guid entityId = Guid.NewGuid();

        await service.WriteAsync(
            "mapping.created",
            "ChannelMapping",
            entityId,
            before: null,
            after: new { Name = "After" });
        await service.WriteAsync(
            "mapping.updated",
            "ChannelMapping",
            entityId,
            before: new { Name = "Before" },
            after: new { Name = "After" });

        IReadOnlyList<Application.Models.AuditLogItem> items = await service.GetRecentAsync();

        Assert.Equal(["mapping.updated", "mapping.created"], items.Select(x => x.Action).ToArray());

        Domain.Entities.AuditLogEntry[] entries = await dbContext.AuditLog
            .OrderBy(x => x.CreatedAt)
            .ToArrayAsync();
        Assert.Null(entries[0].BeforeJson);
        Assert.Contains("\"name\":\"After\"", entries[0].AfterJson, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"Before\"", entries[1].BeforeJson, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"After\"", entries[1].AfterJson, StringComparison.Ordinal);
        Assert.Equal(entityId, entries[1].EntityId);
    }

    [Fact]
    public async Task GetRecentAsync_LimitsResultsToHundred()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        AuditLogService service = new(dbContext);

        for (int index = 0; index < 105; index++)
        {
            dbContext.AuditLog.Add(
                new Domain.Entities.AuditLogEntry
                {
                    Action = $"action-{index:D3}",
                    EntityType = "Test",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(index),
                });
        }

        await dbContext.SaveChangesAsync();

        IReadOnlyList<Application.Models.AuditLogItem> items = await service.GetRecentAsync();

        Assert.Equal(100, items.Count);
        Assert.Equal("action-104", items[0].Action);
        Assert.Equal("action-005", items[^1].Action);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CopyCatDbContext(options);
    }
}
