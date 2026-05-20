using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Messages;

/// <summary>
/// Represents a stored Telegram message projected into the normalized shape used by filters and rewrites.
/// </summary>
public sealed record NormalizedTelegramMessage(
    Guid MessageId,
    Guid SessionId,
    Guid SourceChannelId,
    long TelegramMessageId,
    DateTimeOffset MessageDate,
    DateTimeOffset? EditDate,
    string? Text,
    string? Caption,
    string NormalizedText,
    IReadOnlyList<AttachmentType> AttachmentTypes,
    IReadOnlyList<string> Links,
    IReadOnlyList<string> TelegramLinks,
    string? GroupedId)
{
    /// <summary>
    /// Gets a value indicating whether the message contains visible text in either the body or caption.
    /// </summary>
    public bool HasText => !string.IsNullOrWhiteSpace(Text) || !string.IsNullOrWhiteSpace(Caption);

    /// <summary>
    /// Creates a normalized message snapshot from a stored message entity.
    /// </summary>
    /// <param name="message">The stored message entity, including any loaded attachments and links.</param>
    /// <returns>A normalized message containing the text, links, and attachment metadata needed by the rules engine.</returns>
    public static NormalizedTelegramMessage FromEntity(StoredMessage message)
    {
        return new NormalizedTelegramMessage(
            message.Id,
            message.SessionId,
            message.SourceChannelId,
            message.TelegramMessageId,
            message.MessageDate,
            message.EditDate,
            message.Text,
            message.Caption,
            message.NormalizedText ?? string.Empty,
            message.Attachments.Select(x => x.AttachmentType).ToArray(),
            message.Links.Select(x => x.Url).ToArray(),
            message.Links.Where(x => x.LinkType == "Telegram").Select(x => x.Url).ToArray(),
            message.GroupedId);
    }
}
