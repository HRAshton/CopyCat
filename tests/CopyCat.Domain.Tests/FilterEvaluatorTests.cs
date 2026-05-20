using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Tests;

public sealed class FilterEvaluatorTests
{
    [Fact]
    public void Evaluate_AcceptsMessage_WhenTextExists()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            42,
            DateTimeOffset.UtcNow,
            null,
            "hello",
            null,
            "hello",
            [],
            [],
            [],
            null);
        FilterSetDefinition filter = new(
            "Text only",
            MappingDefaultPolicy.Allow,
            new HasTextCondition(true, "has_text"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.True(decision.Accepted);
        Assert.Equal("has_text", decision.MatchedRuleId);
    }

    [Theory]
    [InlineData(MappingDefaultPolicy.Allow, true)]
    [InlineData(MappingDefaultPolicy.Reject, false)]
    public void Evaluate_WithoutRoot_UsesDefaultPolicy(
        MappingDefaultPolicy defaultPolicy,
        bool expectedAccepted)
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(
            text: null,
            caption: null,
            normalizedText: string.Empty);
        FilterSetDefinition filter = new("Default policy", defaultPolicy, null);

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.Equal(expectedAccepted, decision.Accepted);
        Assert.Contains("default policy", decision.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_AllConditionTypes_AreProcessed()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(
            text: "Promo @copycat",
            caption: "Caption",
            normalizedText: "promo @copycat caption",
            attachmentTypes: [AttachmentType.Photo],
            links: ["https://example.com", "https://t.me/channel/10"],
            telegramLinks: ["https://t.me/channel/10"],
            editDate: DateTimeOffset.UtcNow);
        FilterSetDefinition filter = new(
            "All checks",
            MappingDefaultPolicy.Reject,
            new ConditionGroup(
                LogicalOperator.And,
                new FilterNode[]
                {
                    new HasTextCondition(true, "text"),
                    new HasAttachmentCondition([AttachmentType.Photo], "attachment"),
                    new ContainsWordsCondition(["promo", "caption"], MatchMode.All, false, true, "words"),
                    new HasTelegramLinkCondition(true, "telegram_link"), new HasAnyUrlCondition(true, "any_url"),
                    new RegexCondition("promo", TextField.Combined, "regex"), new LengthCondition(5, 100, "length"),
                    new IsEditedCondition(true, "edited"),
                },
                "group"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.True(decision.Accepted);
        Assert.Equal("text", decision.MatchedRuleId);
        Assert.Contains(decision.Trace, trace => trace.Contains("Attachment match = True.", StringComparison.Ordinal));
        Assert.Contains(
            decision.Trace,
            trace => trace.Contains("Telegram link present = True", StringComparison.Ordinal));
        Assert.Contains(decision.Trace, trace => trace.Contains("Length", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_BlacklistWords_RejectsMatchingMessage()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(normalizedText: "contains forbidden word");
        FilterSetDefinition filter = new(
            "Blacklist",
            MappingDefaultPolicy.Allow,
            new ContainsWordsCondition(["forbidden"], MatchMode.Any, false, false, "blacklist"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.False(decision.Accepted);
        Assert.Equal("blacklist", decision.MatchedRuleId);
    }

    [Fact]
    public void Evaluate_OrGroup_AcceptsWhenAnyConditionMatches()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(text: null, caption: null, normalizedText: "hello");
        FilterSetDefinition filter = new(
            "Or group",
            MappingDefaultPolicy.Reject,
            new ConditionGroup(
                LogicalOperator.Or,
                new FilterNode[]
                {
                    new HasTextCondition(true, "text"),
                    new RegexCondition("hello", TextField.NormalizedText, "regex"),
                },
                "group"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.True(decision.Accepted);
        Assert.Equal("regex", decision.MatchedRuleId);
    }

    [Fact]
    public void Evaluate_HasAttachmentWithoutSpecificTypes_AcceptsAnyAttachment()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(attachmentTypes: [AttachmentType.Document]);
        FilterSetDefinition filter = new(
            "Any attachment",
            MappingDefaultPolicy.Reject,
            new HasAttachmentCondition([], "attachment"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.True(decision.Accepted);
        Assert.Equal("attachment", decision.MatchedRuleId);
    }

    [Fact]
    public void Evaluate_CaseSensitiveWords_UsesOriginalCasing()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(normalizedText: "Promo only");
        FilterSetDefinition filter = new(
            "Case sensitive",
            MappingDefaultPolicy.Reject,
            new ContainsWordsCondition(["promo"], MatchMode.Any, true, true, "words"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.False(decision.Accepted);
        Assert.Equal("words", decision.MatchedRuleId);
    }

    [Fact]
    public void Evaluate_RegexAndLength_HandleMissingTextAndCaption()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage(text: null, caption: "caption", normalizedText: "caption");

        FilterDecision textDecision = evaluator.Evaluate(
            message,
            new FilterSetDefinition(
                "Regex text",
                MappingDefaultPolicy.Reject,
                new RegexCondition("caption", TextField.Text, "text-regex")));
        FilterDecision captionDecision = evaluator.Evaluate(
            message,
            new FilterSetDefinition(
                "Regex caption",
                MappingDefaultPolicy.Reject,
                new RegexCondition("caption", TextField.Caption, "caption-regex")));
        FilterDecision lengthDecision = evaluator.Evaluate(
            message,
            new FilterSetDefinition(
                "Length max only",
                MappingDefaultPolicy.Reject,
                new LengthCondition(null, 10, "length")));

        Assert.False(textDecision.Accepted);
        Assert.True(captionDecision.Accepted);
        Assert.True(lengthDecision.Accepted);
    }

    [Fact]
    public void Evaluate_UnknownNode_IsRejectedAndTraced()
    {
        FilterEvaluator evaluator = new();
        NormalizedTelegramMessage message = CreateMessage();
        FilterSetDefinition filter = new(
            "Unknown node",
            MappingDefaultPolicy.Allow,
            new UnknownNode("unknown"));

        FilterDecision decision = evaluator.Evaluate(message, filter);

        Assert.False(decision.Accepted);
        Assert.Equal("unknown", decision.MatchedRuleId);
        Assert.Contains(decision.Trace, trace => trace.Contains("Unknown filter node", StringComparison.Ordinal));
    }

    private static NormalizedTelegramMessage CreateMessage(
        string? text = "hello",
        string? caption = null,
        string normalizedText = "hello",
        IReadOnlyList<AttachmentType>? attachmentTypes = null,
        IReadOnlyList<string>? links = null,
        IReadOnlyList<string>? telegramLinks = null,
        DateTimeOffset? editDate = null)
    {
        return new NormalizedTelegramMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            42,
            DateTimeOffset.UtcNow,
            editDate,
            text,
            caption,
            normalizedText,
            attachmentTypes ?? [],
            links ?? [],
            telegramLinks ?? [],
            null);
    }

    private sealed record UnknownNode(string? RuleId = null) : FilterNode(RuleId);
}
