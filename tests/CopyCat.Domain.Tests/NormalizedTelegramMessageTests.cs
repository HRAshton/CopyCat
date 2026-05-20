using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Tests;

public sealed class NormalizedTelegramMessageTests
{
    [Fact]
    public void FromEntity_MapsAttachments_AndTelegramLinks()
    {
        Guid messageId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid sourceChannelId = Guid.NewGuid();
        StoredMessage entity = new()
        {
            Id = messageId,
            SessionId = sessionId,
            SourceChannelId = sourceChannelId,
            TelegramMessageId = 42,
            MessageDate = DateTimeOffset.UtcNow,
            Caption = "Caption",
            NormalizedText = "caption",
            GroupedId = "album-1",
            Attachments =
            [
                new MessageAttachment { AttachmentType = AttachmentType.Photo },
                new MessageAttachment { AttachmentType = AttachmentType.Video }
            ],
            Links =
            [
                new MessageLink { Url = "https://example.com", LinkType = "External" },
                new MessageLink { Url = "https://t.me/source/42", LinkType = "Telegram" }
            ],
        };

        NormalizedTelegramMessage normalized = NormalizedTelegramMessage.FromEntity(entity);

        Assert.Equal(messageId, normalized.MessageId);
        Assert.Equal(sessionId, normalized.SessionId);
        Assert.Equal(sourceChannelId, normalized.SourceChannelId);
        Assert.True(normalized.HasText);
        Assert.Equal("album-1", normalized.GroupedId);
        Assert.Equal([AttachmentType.Photo, AttachmentType.Video], normalized.AttachmentTypes);
        Assert.Equal(
            ["https://example.com", "https://t.me/source/42"],
            normalized.Links);
        Assert.Equal(["https://t.me/source/42"], normalized.TelegramLinks);
    }

    [Fact]
    public void FromEntity_UsesEmptyNormalizedText_AndTreatsWhitespaceAsNoVisibleText()
    {
        StoredMessage entity = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            SourceChannelId = Guid.NewGuid(),
            TelegramMessageId = 43,
            MessageDate = DateTimeOffset.UtcNow,
            Text = " ",
            Caption = null,
            NormalizedText = null,
        };

        NormalizedTelegramMessage normalized = NormalizedTelegramMessage.FromEntity(entity);

        Assert.False(normalized.HasText);
        Assert.Equal(string.Empty, normalized.NormalizedText);
        Assert.Empty(normalized.AttachmentTypes);
        Assert.Empty(normalized.Links);
        Assert.Empty(normalized.TelegramLinks);
    }
}
