using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Rewrites normalized messages.
/// </summary>
public interface IMessageRewriter
{
    /// <summary>
    /// Rewrites the specified message.
    /// </summary>
    /// <param name="message">The message to rewrite.</param>
    /// <param name="rules">The rewrite rules to apply.</param>
    /// <returns>The rewrite result.</returns>
    RewriteResult Rewrite(NormalizedTelegramMessage message, RewriteRuleSet rules);
}
