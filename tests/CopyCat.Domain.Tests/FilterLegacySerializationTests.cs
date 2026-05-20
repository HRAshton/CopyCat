using System.Text.Json;

using CopyCat.Domain.Filters;

namespace CopyCat.Domain.Tests;

public sealed class FilterLegacySerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("""{"children":[],"operator":0}""", typeof(ConditionGroup))]
    [InlineData("""{"expected":true}""", typeof(HasTextCondition))]
    [InlineData("""{"expected":true,"pattern":"ignored"}""", typeof(IsEditedCondition))]
    [InlineData("""{"types":[0]}""", typeof(HasAttachmentCondition))]
    [InlineData(
        """{"words":["promo"],"matchMode":0,"caseSensitive":false,"isWhitelist":true}""",
        typeof(ContainsWordsCondition))]
    [InlineData("""{"pattern":"promo","field":3}""", typeof(RegexCondition))]
    [InlineData("""{"minimum":1,"maximum":10}""", typeof(LengthCondition))]
    [InlineData("""{}""", typeof(HasTelegramLinkCondition))]
    [InlineData("""{"ruleId":"telegram-only"}""", typeof(HasTelegramLinkCondition))]
    public void Deserialize_LegacyNodeShape_ReturnsExpectedNode(string json, Type expectedType)
    {
        FilterNode? node = JsonSerializer.Deserialize<FilterNode>(json, Options);

        Assert.NotNull(node);
        Assert.IsType(expectedType, node, exactMatch: true);
    }

    [Fact]
    public void Deserialize_UnsupportedLegacyNode_ThrowsJsonException()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FilterNode>("""{"unexpected":true}""", Options));

        Assert.Contains("Unsupported legacy filter node shape.", exception.Message);
    }

    [Fact]
    public void Deserialize_EmptyNodePayload_ThrowsJsonException()
    {
        FilterNodeJsonConverter converter = new();
        Utf8JsonReader reader = new("null"u8.ToArray());

        reader.Read();

        JsonException exception;
        try
        {
            converter.Read(ref reader, typeof(FilterNode), Options);
            throw new Xunit.Sdk.XunitException("Expected JsonException to be thrown.");
        }
        catch (JsonException caught)
        {
            exception = caught;
        }

        Assert.Contains("Filter node JSON is empty.", exception.Message);
    }
}
