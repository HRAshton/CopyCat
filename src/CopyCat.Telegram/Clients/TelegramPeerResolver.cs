using CopyCat.Domain.Entities;

using TL;

using WTelegram;

namespace CopyCat.Telegram.Clients;

internal static class TelegramPeerResolver
{
    internal static async Task<InputPeer> ResolvePeerAsync(
        Client client,
        TelegramChannel channel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Messages_Chats? chats = await client.Messages_GetAllChats();
        if (chats.chats.TryGetValue(channel.TelegramChannelId, out ChatBase? chatBase))
        {
            return chatBase;
        }

        if (!string.IsNullOrWhiteSpace(channel.Username))
        {
            Contacts_ResolvedPeer? resolved = await client.Contacts_ResolveUsername(channel.Username.TrimStart('@'));
            return resolved.Chat ?? throw new InvalidOperationException($"Could not resolve @{channel.Username}.");
        }

        throw new InvalidOperationException(
            $"Unable to resolve Telegram peer '{channel.Title}' ({channel.TelegramChannelId}).");
    }
}
