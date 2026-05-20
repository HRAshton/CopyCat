using System.Text.Json;

using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Tests;

public sealed class RewriteSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RewriteRuleSet_RoundTrips_WithPolymorphicOperations()
    {
        RewriteRuleSet rules = new(
            new RewriteOperation[]
            {
                new RemoveLinksOperation(), new RegexReplaceOperation("source", "target"),
                new AppendFooterOperation("Footer"),
            });

        string json = JsonSerializer.Serialize(rules, Options);
        RewriteRuleSet? restored = JsonSerializer.Deserialize<RewriteRuleSet>(json, Options);

        Assert.NotNull(restored);
        Assert.Collection(
            restored!.EffectiveOperations,
            operation => Assert.IsType<RemoveLinksOperation>(operation),
            operation => Assert.IsType<RegexReplaceOperation>(operation),
            operation => Assert.IsType<AppendFooterOperation>(operation));
    }

    [Theory]
    [InlineData("""{"$type":"replace_exact_text","search":"old","replacement":"new"}""", typeof(ReplaceExactTextOperation))]
    [InlineData("""{"$type":"regex_replace","pattern":"old","replacement":"new"}""", typeof(RegexReplaceOperation))]
    [InlineData("""{"$type":"remove_links"}""", typeof(RemoveLinksOperation))]
    [InlineData("""{"$type":"remove_mentions"}""", typeof(RemoveTelegramMentionsOperation))]
    [InlineData("""{"$type":"replace_telegram_links","search":"https://t.me/a","replacement":"https://t.me/b"}""", typeof(ReplaceTelegramChannelLinksOperation))]
    [InlineData("""{"$type":"append_footer","text":"Footer"}""", typeof(AppendFooterOperation))]
    [InlineData("""{"$type":"prepend_header","text":"Header"}""", typeof(PrependHeaderOperation))]
    [InlineData("""{"$type":"strip_caption"}""", typeof(StripCaptionOperation))]
    [InlineData("""{"$type":"strip_all_text"}""", typeof(StripAllTextOperation))]
    [InlineData("""{"$type":"text_only_output"}""", typeof(TextOnlyOutputOperation))]
    public void Deserialize_DiscriminatorOperation_ReturnsExpectedOperationType(string json, Type expectedType)
    {
        RewriteOperation? operation = JsonSerializer.Deserialize<RewriteOperation>(json, Options);

        Assert.NotNull(operation);
        Assert.IsType(expectedType, operation, exactMatch: true);
    }
}
