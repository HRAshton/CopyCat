using System.Text.Json;

using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

using TL;

namespace CopyCat.Telegram.Clients;

internal static class TelegramMessageMapper
{
    internal static StoredMessage MapMessage(TelegramSession session, TelegramChannel sourceChannel, Message message)
    {
        List<MessageAttachment> attachments = [];
        if (message.media is MessageMediaPhoto)
        {
            attachments.Add(new MessageAttachment { AttachmentType = AttachmentType.Photo });
        }
        else if (message.media is MessageMediaDocument)
        {
            attachments.Add(new MessageAttachment { AttachmentType = AttachmentType.Document });
        }

        List<MessageLink> links = ExtractLinks(message.message);
        return new StoredMessage
        {
            SessionId = session.Id,
            SourceChannelId = sourceChannel.Id,
            TelegramMessageId = message.ID,
            GroupedId = message.grouped_id == 0 ? null : message.grouped_id.ToString(),
            MessageDate = new DateTimeOffset(message.Date),
            EditDate = message.edit_date == default ? null : new DateTimeOffset(message.edit_date),
            Text = message.message,
            Caption = message.message,
            NormalizedText = (message.message ?? string.Empty).Trim(),
            RawJson = JsonSerializer.Serialize(message),
            Attachments = attachments,
            Links = links,
        };
    }

    private static List<MessageLink> ExtractLinks(string? text)
    {
        List<MessageLink> links = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            return links;
        }

        foreach (string token in text.Split(
                     ' ',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                links.Add(
                    new MessageLink
                    {
                        Url = token,
                        LinkType = token.Contains("t.me/", StringComparison.OrdinalIgnoreCase) ? "Telegram" : "Url",
                    });
            }
            else if (token.Contains("t.me/", StringComparison.OrdinalIgnoreCase))
            {
                links.Add(new MessageLink { Url = token, LinkType = "Telegram" });
            }
        }

        return links;
    }
}
