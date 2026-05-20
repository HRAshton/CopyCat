using System.Text.RegularExpressions;

using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Applies rewrite operations to normalized messages.
/// </summary>
public sealed class MessageRewriter : IMessageRewriter
{
    private static readonly Regex UrlRegex = new(
        @"https?://\S+|t\.me/\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MentionRegex = new(@"@\w+", RegexOptions.Compiled);

    /// <inheritdoc />
    public RewriteResult Rewrite(NormalizedTelegramMessage message, RewriteRuleSet rules)
    {
        List<string> trace = [];
        string? text = message.Text;
        string? caption = message.Caption;
        bool dropMedia = false;

        foreach (RewriteOperation operation in rules.EffectiveOperations)
        {
            switch (operation)
            {
                case ReplaceExactTextOperation exact:
                    text = text?.Replace(exact.Search, exact.Replacement, StringComparison.OrdinalIgnoreCase);
                    caption = caption?.Replace(exact.Search, exact.Replacement, StringComparison.OrdinalIgnoreCase);
                    trace.Add($"Replaced exact text '{exact.Search}'.");
                    break;
                case RegexReplaceOperation regex:
                    text = text is null
                        ? null
                        : Regex.Replace(text, regex.Pattern, regex.Replacement, RegexOptions.IgnoreCase);
                    caption = caption is null
                        ? null
                        : Regex.Replace(caption, regex.Pattern, regex.Replacement, RegexOptions.IgnoreCase);
                    trace.Add($"Applied regex '{regex.Pattern}'.");
                    break;
                case RemoveLinksOperation:
                    text = text is null ? null : UrlRegex.Replace(text, string.Empty).Trim();
                    caption = caption is null ? null : UrlRegex.Replace(caption, string.Empty).Trim();
                    trace.Add("Removed links.");
                    break;
                case RemoveTelegramMentionsOperation:
                    text = text is null ? null : MentionRegex.Replace(text, string.Empty).Trim();
                    caption = caption is null ? null : MentionRegex.Replace(caption, string.Empty).Trim();
                    trace.Add("Removed mentions.");
                    break;
                case ReplaceTelegramChannelLinksOperation replaceLinks:
                    text = text?.Replace(
                        replaceLinks.Search,
                        replaceLinks.Replacement,
                        StringComparison.OrdinalIgnoreCase);
                    caption = caption?.Replace(
                        replaceLinks.Search,
                        replaceLinks.Replacement,
                        StringComparison.OrdinalIgnoreCase);
                    trace.Add("Replaced Telegram links.");
                    break;
                case AppendFooterOperation footer:
                    text = string.IsNullOrWhiteSpace(text) ? footer.Text : $"{text}\n\n{footer.Text}";
                    trace.Add("Appended footer.");
                    break;
                case PrependHeaderOperation header:
                    text = string.IsNullOrWhiteSpace(text) ? header.Text : $"{header.Text}\n\n{text}";
                    trace.Add("Prepended header.");
                    break;
                case StripCaptionOperation:
                    caption = null;
                    trace.Add("Stripped caption.");
                    break;
                case StripAllTextOperation:
                    text = null;
                    caption = null;
                    trace.Add("Stripped all text.");
                    break;
                case TextOnlyOutputOperation:
                    dropMedia = true;
                    trace.Add("Forced text-only output.");
                    break;
            }
        }

        return new RewriteResult(text, caption, dropMedia, trace);
    }
}
