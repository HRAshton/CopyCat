using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class MessageHistoryServiceTests
{
    [Fact]
    public async Task GetRecentMessagesAsync_ReturnsNewestMessagesFirstAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceA = CreateChannel("A");
        TelegramChannel sourceB = CreateChannel("B");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await dbContext.AddRangeAsync(
            sourceA,
            sourceB,
            CreateMessage(sourceA.Id, 10, now, "old", [AttachmentType.Photo]),
            CreateMessage(sourceA.Id, 12, now, "newest-same-date", [AttachmentType.Video]),
            CreateMessage(sourceB.Id, 11, now.AddMinutes(1), "overall-newest", []));
        await dbContext.SaveChangesAsync();

        MessageHistoryService service = new(dbContext);

        IReadOnlyList<CopyCat.Application.Models.MessageHistoryItem> results = await service.GetRecentMessagesAsync(
            null,
            3);

        Assert.Collection(
            results,
            item => Assert.Equal(11, item.TelegramMessageId),
            item => Assert.Equal(12, item.TelegramMessageId),
            item => Assert.Equal(10, item.TelegramMessageId));
        Assert.Equal("Video", results[1].AttachmentSummary);
    }

    [Fact]
    public async Task GetNormalizedMessageAsync_AndChannelFiltering_WorkAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceA = CreateChannel("A");
        TelegramChannel sourceB = CreateChannel("B");
        StoredMessage message = CreateMessage(
            sourceA.Id,
            10,
            DateTimeOffset.UtcNow,
            "Text",
            [AttachmentType.Photo],
            [new MessageLink { Url = "https://t.me/source/10", LinkType = "Telegram" }]);

        await dbContext.AddRangeAsync(
            sourceA,
            sourceB,
            message,
            CreateMessage(sourceB.Id, 20, DateTimeOffset.UtcNow, "Other"));
        await dbContext.SaveChangesAsync();

        MessageHistoryService service = new(dbContext);

        IReadOnlyList<CopyCat.Application.Models.MessageHistoryItem> filtered = await service.GetRecentMessagesAsync(
            sourceA.Id,
            10);
        CopyCat.Domain.Messages.NormalizedTelegramMessage? normalized =
            await service.GetNormalizedMessageAsync(message.Id);
        CopyCat.Domain.Messages.NormalizedTelegramMessage? missing =
            await service.GetNormalizedMessageAsync(Guid.NewGuid());

        CopyCat.Application.Models.MessageHistoryItem historyItem = Assert.Single(filtered);
        Assert.Equal(10, historyItem.TelegramMessageId);
        Assert.NotNull(normalized);
        Assert.Equal(["https://t.me/source/10"], normalized!.TelegramLinks);
        Assert.Null(missing);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CopyCatDbContext(options);
    }

    private static TelegramChannel CreateChannel(string title)
    {
        return new TelegramChannel
        {
            SessionId = Guid.NewGuid(),
            Title = title,
            TelegramChannelId = Random.Shared.NextInt64(1, int.MaxValue),
            AccessHash = Random.Shared.NextInt64(1, int.MaxValue).ToString(),
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
    }

    private static StoredMessage CreateMessage(
        Guid sourceChannelId,
        long telegramMessageId,
        DateTimeOffset messageDate,
        string text,
        IReadOnlyList<AttachmentType>? attachments = null,
        IReadOnlyList<MessageLink>? links = null)
    {
        return new StoredMessage
        {
            SessionId = Guid.NewGuid(),
            SourceChannelId = sourceChannelId,
            TelegramMessageId = telegramMessageId,
            MessageDate = messageDate,
            Text = text,
            NormalizedText = text.ToLowerInvariant(),
            Attachments = attachments?.Select(type => new MessageAttachment { AttachmentType = type }).ToList() ?? [],
            Links = links?.ToList() ?? [],
        };
    }
}
