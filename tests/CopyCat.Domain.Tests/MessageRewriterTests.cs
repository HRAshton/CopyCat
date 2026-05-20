using CopyCat.Domain.Messages;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Tests;

public sealed class MessageRewriterTests
{
    [Fact]
    public void Rewrite_RemovesLinks_AndAppendsFooter()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow,
            null,
            "Visit https://example.com",
            null,
            "Visit https://example.com",
            [],
            ["https://example.com"],
            [],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet(
                new RewriteOperation[] { new RemoveLinksOperation(), new AppendFooterOperation("Footer") }));

        Assert.DoesNotContain("https://example.com", result.Text);
        Assert.Contains("Footer", result.Text);
    }

    [Fact]
    public void Rewrite_AppliesAllOperationTypes()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow,
            null,
            "Hello @copycat https://t.me/source Promo",
            "Caption https://example.com",
            "hello @copycat https://t.me/source promo",
            [],
            ["https://t.me/source", "https://example.com"],
            ["https://t.me/source"],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet(
                new RewriteOperation[]
                {
                    new ReplaceExactTextOperation("Promo", "News"), new RegexReplaceOperation("Hello", "Hi"),
                    new RemoveTelegramMentionsOperation(),
                    new ReplaceTelegramChannelLinksOperation("https://t.me/source", "https://t.me/target"),
                    new PrependHeaderOperation("Header"), new StripCaptionOperation(),
                    new TextOnlyOutputOperation(),
                }));

        Assert.NotNull(result.Text);
        Assert.Contains("Header", result.Text);
        Assert.Contains("Hi", result.Text);
        Assert.Contains("News", result.Text);
        Assert.DoesNotContain("@copycat", result.Text);
        Assert.Contains("https://t.me/target", result.Text);
        Assert.Null(result.Caption);
        Assert.True(result.DropMedia);
        Assert.Contains(result.Trace, trace => trace.Contains("Prepended header.", StringComparison.Ordinal));
        Assert.Contains(result.Trace, trace => trace.Contains("Removed mentions.", StringComparison.Ordinal));
    }

    [Fact]
    public void Rewrite_WhenTextIsBlank_UsesHeaderOrFooterTextDirectly()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            DateTimeOffset.UtcNow,
            null,
            " ",
            null,
            string.Empty,
            [],
            [],
            [],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet(
                new RewriteOperation[] { new AppendFooterOperation("Footer"), new PrependHeaderOperation("Header"), }));

        Assert.Equal("Header\n\nFooter", result.Text);
    }

    [Fact]
    public void Rewrite_StripAllText_ClearsTextAndCaption()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            DateTimeOffset.UtcNow,
            null,
            "Text",
            "Caption",
            "text caption",
            [],
            [],
            [],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet([new StripAllTextOperation()]));

        Assert.Null(result.Text);
        Assert.Null(result.Caption);
    }

    [Fact]
    public void Rewrite_WhenTextIsNull_TransformsCaptionOnlyBranches()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            4,
            DateTimeOffset.UtcNow,
            null,
            null,
            "Hello @copycat https://t.me/source Promo",
            "hello @copycat https://t.me/source promo",
            [],
            ["https://t.me/source"],
            ["https://t.me/source"],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet(
                [
                    new ReplaceExactTextOperation("Promo", "News"),
                    new RegexReplaceOperation("Hello", "Hi"),
                    new RemoveLinksOperation(),
                    new RemoveTelegramMentionsOperation(),
                    new ReplaceTelegramChannelLinksOperation("https://t.me/source", "https://t.me/target"),
                ]));

        Assert.Null(result.Text);
        Assert.NotNull(result.Caption);
        Assert.Contains("Hi", result.Caption);
        Assert.Contains("News", result.Caption);
        Assert.DoesNotContain("@copycat", result.Caption);
        Assert.DoesNotContain("https://t.me/source", result.Caption);
    }

    [Fact]
    public void Rewrite_WhenCaptionIsNull_LeavesCaptionNullAndCanSetConnectedTextTimestamp()
    {
        MessageRewriter rewriter = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            DateTimeOffset.UtcNow,
            null,
            "Hello https://example.com",
            null,
            "hello https://example.com",
            [],
            ["https://example.com"],
            [],
            null);

        RewriteResult result = rewriter.Rewrite(
            message,
            new RewriteRuleSet(
                [
                    new RegexReplaceOperation("Hello", "Hi"),
                    new RemoveLinksOperation(),
                    new AppendFooterOperation("Footer"),
                ]));

        Assert.NotNull(result.Text);
        Assert.Contains("Hi", result.Text);
        Assert.Contains("Footer", result.Text);
        Assert.Null(result.Caption);
    }
}
