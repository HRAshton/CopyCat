namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Removes Telegram mentions from message text.
/// </summary>
public sealed record RemoveTelegramMentionsOperation() : RewriteOperation;
