using System.Text.Json;

using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Tests;

public sealed class RewriteLegacySerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("""{"search":"old","replacement":"new"}""", typeof(ReplaceExactTextOperation))]
    [InlineData("""{"pattern":"promo","replacement":"clean"}""", typeof(RegexReplaceOperation))]
    [InlineData("""{"text":"Footer"}""", typeof(AppendFooterOperation))]
    [InlineData("""{}""", typeof(RemoveLinksOperation))]
    public void Deserialize_LegacyOperationShape_ReturnsExpectedOperation(string json, Type expectedType)
    {
        RewriteOperation? operation = JsonSerializer.Deserialize<RewriteOperation>(json, Options);

        Assert.NotNull(operation);
        Assert.IsType(expectedType, operation, exactMatch: true);
    }

    [Fact]
    public void Deserialize_UnsupportedLegacyOperation_ThrowsJsonException()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RewriteOperation>("""{"unexpected":true}""", Options));

        Assert.Contains("Unsupported legacy rewrite operation shape.", exception.Message);
    }

    [Fact]
    public void Deserialize_EmptyOperationPayload_ThrowsJsonException()
    {
        RewriteOperationJsonConverter converter = new();
        Utf8JsonReader reader = new("null"u8.ToArray());

        reader.Read();

        JsonException exception;
        try
        {
            converter.Read(ref reader, typeof(RewriteOperation), Options);
            throw new Xunit.Sdk.XunitException("Expected JsonException to be thrown.");
        }
        catch (JsonException caught)
        {
            exception = caught;
        }

        Assert.Contains("Rewrite operation JSON is empty.", exception.Message);
    }
}
