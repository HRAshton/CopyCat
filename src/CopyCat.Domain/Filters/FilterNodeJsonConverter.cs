using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Deserializes persisted filter node payloads into the correct concrete filter node type.
/// </summary>
public sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
{
    /// <summary>
    /// Reads a filter node from JSON and resolves the concrete node type from the discriminator or legacy shape.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the filter node payload.</param>
    /// <param name="typeToConvert">The declared target type being converted.</param>
    /// <param name="options">The serializer options to use for nested node deserialization.</param>
    /// <returns>The deserialized filter node instance.</returns>
    public override FilterNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonObject node = JsonNode.Parse(ref reader)?.AsObject() ??
                          throw new JsonException("Filter node JSON is empty.");
        string? discriminator = node["$type"]?.GetValue<string>();

        return discriminator switch
        {
            "condition_group" => Deserialize<ConditionGroup>(node, options),
            "has_text" => Deserialize<HasTextCondition>(node, options),
            "has_attachment" => Deserialize<HasAttachmentCondition>(node, options),
            "contains_words" => Deserialize<ContainsWordsCondition>(node, options),
            "has_telegram_link" => Deserialize<HasTelegramLinkCondition>(node, options),
            "has_any_url" => Deserialize<HasAnyUrlCondition>(node, options),
            "regex" => Deserialize<RegexCondition>(node, options),
            "length" => Deserialize<LengthCondition>(node, options),
            "is_edited" => Deserialize<IsEditedCondition>(node, options),
            _ => InferLegacyNode(node, options),
        };
    }

    /// <summary>
    /// Writes a filter node using its runtime type so derived properties are preserved.
    /// </summary>
    /// <param name="writer">The JSON writer receiving the serialized payload.</param>
    /// <param name="value">The filter node instance to serialize.</param>
    /// <param name="options">The serializer options to use for writing.</param>
    public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static FilterNode InferLegacyNode(JsonObject node, JsonSerializerOptions options)
    {
        if (node.ContainsKey("children") && node.ContainsKey("operator"))
        {
            return Deserialize<ConditionGroup>(node, options);
        }

        if (node.ContainsKey("expected") && !node.ContainsKey("minimum") && !node.ContainsKey("maximum"))
        {
            return node.ContainsKey("pattern")
                ? Deserialize<IsEditedCondition>(node, options)
                : Deserialize<HasTextCondition>(node, options);
        }

        if (node.ContainsKey("types"))
        {
            return Deserialize<HasAttachmentCondition>(node, options);
        }

        if (node.ContainsKey("words"))
        {
            return Deserialize<ContainsWordsCondition>(node, options);
        }

        if (node.ContainsKey("pattern"))
        {
            return Deserialize<RegexCondition>(node, options);
        }

        if (node.ContainsKey("minimum") || node.ContainsKey("maximum"))
        {
            return Deserialize<LengthCondition>(node, options);
        }

        if (node.Count == 0 || (node.Count == 1 && node.ContainsKey("ruleId")))
        {
            return Deserialize<HasTelegramLinkCondition>(node, options);
        }

        throw new JsonException("Unsupported legacy filter node shape.");
    }

    private static T Deserialize<T>(JsonNode node, JsonSerializerOptions options)
        where T : FilterNode
    {
        return node.Deserialize<T>(options) ?? throw new JsonException($"Failed to deserialize {typeof(T).Name}.");
    }
}
