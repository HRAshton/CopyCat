using System.Text.Json;

using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

using TL;

namespace CopyCat.Telegram.Clients;

internal static class TelegramChannelMapper
{
    internal static List<TelegramChannel> ExtractChannels(TelegramSession session, dynamic dialogs)
    {
        List<TelegramChannel> channels = [];

        foreach (dynamic? dialog in dialogs.dialogs)
        {
            switch (dialogs.UserOrChat(dialog))
            {
                case Channel { IsActive: true } channel:
                    channels.Add(MapChannel(session, channel));
                    break;
                case Chat { IsActive: true } chat:
                    channels.Add(
                        new TelegramChannel
                        {
                            SessionId = session.Id,
                            TelegramChannelId = chat.id,
                            Title = chat.title,
                            ChannelType = TelegramChannelType.Group,
                            CanAdmin = true,
                            CanPost = true,
                            CanCreateRelatedTargets = true,
                            DiscoveredAt = DateTimeOffset.UtcNow,
                            RawJson = JsonSerializer.Serialize(chat),
                        });
                    break;
            }
        }

        return channels;
    }

    internal static TelegramChannel ExtractCreatedChannel(TelegramSession session, UpdatesBase updates)
    {
        foreach (ChatBase chatBase in EnumerateChats(updates))
        {
            if (chatBase is Channel { IsActive: true } channel)
            {
                return MapChannel(session, channel);
            }
        }

        throw new InvalidOperationException("Telegram did not return the created channel payload.");
    }

    private static IEnumerable<ChatBase> EnumerateChats(UpdatesBase updates)
    {
        return updates switch
        {
            Updates typedUpdates => typedUpdates.chats.Values,
            UpdatesCombined typedCombined => typedCombined.chats.Values,
            _ => [],
        };
    }

    private static TelegramChannel MapChannel(TelegramSession session, Channel channel)
    {
        return new TelegramChannel
        {
            SessionId = session.Id,
            TelegramChannelId = channel.id,
            AccessHash = channel.access_hash.ToString(),
            Title = channel.title,
            Username = channel.username,
            ChannelType = TelegramChannelType.BroadcastChannel,
            CanPost = channel.admin_rights is not null,
            CanAdmin = channel.admin_rights is not null,
            CanCreateRelatedTargets = channel.admin_rights is not null,
            DiscoveredAt = DateTimeOffset.UtcNow,
            RawJson = JsonSerializer.Serialize(channel),
        };
    }
}
