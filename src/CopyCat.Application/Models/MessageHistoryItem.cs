namespace CopyCat.Application.Models;

/// <summary>
/// Represents an item in message history.
/// </summary>
public sealed record MessageHistoryItem(
    Guid Id,
    DateTimeOffset MessageDate,
    string SourceChannelTitle,
    long TelegramMessageId,
    string? Preview,
    string AttachmentSummary,
    string Decision,
    string? RewritePreview);
