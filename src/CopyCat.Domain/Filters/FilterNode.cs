using System.Text.Json.Serialization;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a node in a filter tree.
/// </summary>
[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode(string? RuleId = null);
