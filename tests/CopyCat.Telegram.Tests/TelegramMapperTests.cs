using System.Diagnostics.CodeAnalysis;

using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Telegram.Clients;

using TL;

namespace CopyCat.Telegram.Tests;

public sealed class TelegramMapperTests
{
    [Fact]
    public void ExtractChannels_IgnoresUnsupportedDialogPayloads()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        FakeDialogs dialogs = new([new object(), "not-a-chat"]);

        List<TelegramChannel> channels = TelegramChannelMapper.ExtractChannels(session, (dynamic)dialogs);

        Assert.Empty(channels);
    }

    [Fact]
    public void ExtractChannels_MapsActiveChannelsAndGroups()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        Channel activeChannel = new() { id = 101, title = "Broadcast", access_hash = 500 };
        activeChannel.flags |= Channel.Flags.broadcast;
        activeChannel.admin_rights = new ChatAdminRights();

        Chat activeChat = new() { id = 201, title = "Group" };
        FakeDialogs dialogs = new([activeChannel, activeChat]);

        List<TelegramChannel> channels = TelegramChannelMapper.ExtractChannels(session, (dynamic)dialogs);

        Assert.Collection(
            channels,
            channel =>
            {
                Assert.Equal(101, channel.TelegramChannelId);
                Assert.Equal("Broadcast", channel.Title);
                Assert.Equal(TelegramChannelType.BroadcastChannel, channel.ChannelType);
                Assert.True(channel.CanAdmin);
                Assert.True(channel.CanPost);
                Assert.True(channel.CanCreateRelatedTargets);
            },
            channel =>
            {
                Assert.Equal(201, channel.TelegramChannelId);
                Assert.Equal("Group", channel.Title);
                Assert.Equal(TelegramChannelType.Group, channel.ChannelType);
                Assert.True(channel.CanAdmin);
                Assert.True(channel.CanPost);
                Assert.True(channel.CanCreateRelatedTargets);
            });
    }

    [Fact]
    public void ExtractCreatedChannel_UsesReturnedUpdates_AndThrowsWhenMissing()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        Channel channel = new() { id = 404, title = "Created", access_hash = 12345 };
        channel.flags |= Channel.Flags.broadcast;

        Updates updates = new() { chats = new Dictionary<long, ChatBase> { [channel.id] = channel } };

        TelegramChannel mapped = TelegramChannelMapper.ExtractCreatedChannel(session, updates);

        Assert.Equal(404, mapped.TelegramChannelId);
        Assert.Equal("Created", mapped.Title);
        Assert.Throws<InvalidOperationException>(() =>
            TelegramChannelMapper.ExtractCreatedChannel(session, new Updates { chats = [] }));
    }

    [Fact]
    public void ExtractCreatedChannel_UsesUpdatesCombinedPayload()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        Channel channel = new() { id = 405, title = "Combined", access_hash = 12346 };
        channel.flags |= Channel.Flags.broadcast;

        UpdatesCombined updates = new() { chats = new Dictionary<long, ChatBase> { [channel.id] = channel } };

        TelegramChannel mapped = TelegramChannelMapper.ExtractCreatedChannel(session, updates);

        Assert.Equal(405, mapped.TelegramChannelId);
        Assert.Equal("Combined", mapped.Title);
    }

    [Fact]
    public void ExtractCreatedChannel_ThrowsForUnknownUpdatesType()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        UpdatesTooLong updates = new();

        Assert.Throws<InvalidOperationException>(() => TelegramChannelMapper.ExtractCreatedChannel(session, updates));
    }

    [Fact]
    public void MapMessage_ExtractsHttpLink_WithCorrectLinkType()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        TelegramChannel channel = new() { Id = Guid.NewGuid(), SessionId = session.Id, TelegramChannelId = 57 };
        Message message = new()
        {
            id = 100,
            message = "http://example.com https://t.me/somechannel",
            date = DateTime.UtcNow,
        };

        StoredMessage mapped = TelegramMessageMapper.MapMessage(session, channel, message);

        Assert.Equal(2, mapped.Links.Count);
        Assert.Contains(mapped.Links, x => x is { Url: "http://example.com", LinkType: "Url" });
        Assert.Contains(mapped.Links, x => x is { Url: "https://t.me/somechannel", LinkType: "Telegram" });
    }

    [Fact]
    public void MapMessage_ReturnsNoAttachments_WhenNoMedia()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        TelegramChannel channel = new() { Id = Guid.NewGuid(), SessionId = session.Id, TelegramChannelId = 58 };
        Message message = new() { id = 101, date = DateTime.UtcNow, };

        StoredMessage mapped = TelegramMessageMapper.MapMessage(session, channel, message);

        Assert.Empty(mapped.Attachments);
        Assert.Empty(mapped.Links);
        Assert.Null(mapped.EditDate);
        Assert.Null(mapped.GroupedId);
    }

    [Fact]
    public void MapMessage_MapsMediaAndExtractsLinks()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        TelegramChannel sourceChannel = new() { Id = Guid.NewGuid(), SessionId = session.Id, TelegramChannelId = 55 };
        DateTime now = DateTime.UtcNow;
        DateTime edited = now.AddMinutes(1);
        Message message = new()
        {
            id = 77,
            message = "https://example.test/path t.me/sample plain",
            grouped_id = 999,
            date = now,
            edit_date = edited,
            media = new MessageMediaDocument(),
        };

        StoredMessage mapped = TelegramMessageMapper.MapMessage(session, sourceChannel, message);

        Assert.Equal(session.Id, mapped.SessionId);
        Assert.Equal(sourceChannel.Id, mapped.SourceChannelId);
        Assert.Equal(77, mapped.TelegramMessageId);
        Assert.Equal("999", mapped.GroupedId);
        Assert.Equal("https://example.test/path t.me/sample plain", mapped.Text);
        Assert.Equal(mapped.Text, mapped.Caption);
        Assert.Equal(mapped.Text, mapped.NormalizedText);
        Assert.Equal(new DateTimeOffset(now), mapped.MessageDate);
        Assert.Equal(new DateTimeOffset(edited), mapped.EditDate);
        Assert.Single(mapped.Attachments);
        Assert.Equal(AttachmentType.Document, mapped.Attachments[0].AttachmentType);
        Assert.Equal(2, mapped.Links.Count);
        Assert.Contains(mapped.Links, x => x is { Url: "https://example.test/path", LinkType: "Url" });
        Assert.Contains(mapped.Links, x => x is { Url: "t.me/sample", LinkType: "Telegram" });
    }

    [Fact]
    public void MapMessage_MapsPhotoAndDefaultsNullishFields()
    {
        TelegramSession session = new() { Id = Guid.NewGuid() };
        TelegramChannel sourceChannel = new() { Id = Guid.NewGuid(), SessionId = session.Id, TelegramChannelId = 56 };
        DateTime now = DateTime.UtcNow;
        Message message = new()
        {
            id = 78,
            message = "  visit t.me/test  ",
            date = now,
            media = new MessageMediaPhoto(),
        };

        StoredMessage mapped = TelegramMessageMapper.MapMessage(session, sourceChannel, message);

        Assert.Null(mapped.GroupedId);
        Assert.Null(mapped.EditDate);
        Assert.Equal("visit t.me/test", mapped.NormalizedText);
        Assert.Single(mapped.Attachments);
        Assert.Equal(AttachmentType.Photo, mapped.Attachments[0].AttachmentType);
        Assert.Single(mapped.Links);
        Assert.Equal("Telegram", mapped.Links[0].LinkType);
    }

    [SuppressMessage(
        "Naming",
        "SA1300:Element should begin with upper-case letter",
        Justification = "Matches mapper's dynamic Telegram payload shape.")]
    [SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Matches mapper's dynamic Telegram payload shape.")]
    public sealed class FakeDialogs
    {
        public FakeDialogs(List<object> values)
        {
            dialogs = values;
        }

        public List<object> dialogs { get; }

        public object? UserOrChat(object? dialog)
        {
            return dialog;
        }
    }
}
