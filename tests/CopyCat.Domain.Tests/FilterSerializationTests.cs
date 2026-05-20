using System.Text.Json;

using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;

namespace CopyCat.Domain.Tests;

public sealed class FilterSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void FilterDefinition_RoundTrips_WithPolymorphicNodes()
    {
        FilterSetDefinition definition = new(
            "Roundtrip",
            MappingDefaultPolicy.Allow,
            new ConditionGroup(
                LogicalOperator.And,
                new FilterNode[]
                {
                    new HasTextCondition(true, "text"), new RegexCondition("promo", TextField.Combined, "regex"),
                    new HasAttachmentCondition([AttachmentType.Photo, AttachmentType.Video], "attachment"),
                },
                "group"));

        string json = JsonSerializer.Serialize(definition, Options);
        FilterSetDefinition? restored = JsonSerializer.Deserialize<FilterSetDefinition>(json, Options);

        Assert.NotNull(restored);
        ConditionGroup group = Assert.IsType<ConditionGroup>(restored!.Root);
        Assert.Collection(
            group.Children,
            child => Assert.IsType<HasTextCondition>(child),
            child => Assert.IsType<RegexCondition>(child),
            child => Assert.IsType<HasAttachmentCondition>(child));
    }

    [Theory]
    [InlineData("""{"$type":"condition_group","operator":0,"children":[]}""", typeof(ConditionGroup))]
    [InlineData("""{"$type":"has_text","expected":true}""", typeof(HasTextCondition))]
    [InlineData("""{"$type":"has_attachment","types":[0]}""", typeof(HasAttachmentCondition))]
    [InlineData("""{"$type":"contains_words","words":["promo"],"matchMode":0,"caseSensitive":false,"isWhitelist":true}""", typeof(ContainsWordsCondition))]
    [InlineData("""{"$type":"has_telegram_link","expected":true}""", typeof(HasTelegramLinkCondition))]
    [InlineData("""{"$type":"has_any_url","expected":true}""", typeof(HasAnyUrlCondition))]
    [InlineData("""{"$type":"regex","pattern":"promo","field":3}""", typeof(RegexCondition))]
    [InlineData("""{"$type":"length","minimum":1,"maximum":10}""", typeof(LengthCondition))]
    [InlineData("""{"$type":"is_edited","expected":true}""", typeof(IsEditedCondition))]
    public void Deserialize_DiscriminatorNode_ReturnsExpectedNodeType(string json, Type expectedType)
    {
        FilterNode? node = JsonSerializer.Deserialize<FilterNode>(json, Options);

        Assert.NotNull(node);
        Assert.IsType(expectedType, node, exactMatch: true);
    }
}
