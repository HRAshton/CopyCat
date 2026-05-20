namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Replaces Telegram channel links.
/// </summary>
public sealed record ReplaceTelegramChannelLinksOperation(string Search, string Replacement) : RewriteOperation;
