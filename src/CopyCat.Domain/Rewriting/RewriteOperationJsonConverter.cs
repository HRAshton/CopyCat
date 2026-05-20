using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Deserializes persisted rewrite operations into the correct concrete operation type.
/// </summary>
public sealed class RewriteOperationJsonConverter : JsonConverter<RewriteOperation>
{
    /// <summary>
    /// Reads a rewrite operation from JSON and resolves the concrete operation type from the discriminator or legacy shape.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the rewrite operation payload.</param>
    /// <param name="typeToConvert">The declared target type being converted.</param>
    /// <param name="options">The serializer options to use for nested operation deserialization.</param>
    /// <returns>The deserialized rewrite operation instance, or <see langword="null"/> when the payload maps to a nullable value.</returns>
    public override RewriteOperation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonObject node = JsonNode.Parse(ref reader)?.AsObject() ??
                          throw new JsonException("Rewrite operation JSON is empty.");
        string? discriminator = node["$type"]?.GetValue<string>();

        return discriminator switch
        {
            "replace_exact_text" => Deserialize<ReplaceExactTextOperation>(node, options),
            "regex_replace" => Deserialize<RegexReplaceOperation>(node, options),
            "remove_links" => Deserialize<RemoveLinksOperation>(node, options),
            "remove_mentions" => Deserialize<RemoveTelegramMentionsOperation>(node, options),
            "replace_telegram_links" => Deserialize<ReplaceTelegramChannelLinksOperation>(node, options),
            "append_footer" => Deserialize<AppendFooterOperation>(node, options),
            "prepend_header" => Deserialize<PrependHeaderOperation>(node, options),
            "strip_caption" => Deserialize<StripCaptionOperation>(node, options),
            "strip_all_text" => Deserialize<StripAllTextOperation>(node, options),
            "text_only_output" => Deserialize<TextOnlyOutputOperation>(node, options),
            _ => InferLegacyOperation(node, options),
        };
    }

    /// <summary>
    /// Writes a rewrite operation using its runtime type so derived properties are preserved.
    /// </summary>
    /// <param name="writer">The JSON writer receiving the serialized payload.</param>
    /// <param name="value">The rewrite operation instance to serialize.</param>
    /// <param name="options">The serializer options to use for writing.</param>
    public override void Write(Utf8JsonWriter writer, RewriteOperation value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static RewriteOperation InferLegacyOperation(JsonObject node, JsonSerializerOptions options)
    {
        if (node.ContainsKey("search") && node.ContainsKey("replacement"))
        {
            return Deserialize<ReplaceExactTextOperation>(node, options);
        }

        if (node.ContainsKey("pattern") && node.ContainsKey("replacement"))
        {
            return Deserialize<RegexReplaceOperation>(node, options);
        }

        if (node.ContainsKey("text"))
        {
            return Deserialize<AppendFooterOperation>(node, options);
        }

        if (node.Count == 0)
        {
            return Deserialize<RemoveLinksOperation>(node, options);
        }

        throw new JsonException("Unsupported legacy rewrite operation shape.");
    }

    private static T Deserialize<T>(JsonNode node, JsonSerializerOptions options)
        where T : RewriteOperation
    {
        return node.Deserialize<T>(options) ?? throw new JsonException($"Failed to deserialize {typeof(T).Name}.");
    }
}
