using System.Text.Json;

using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CopyCat.Infrastructure.Data;

/// <summary>
/// Creates shared Entity Framework value converters for JSON-backed domain types.
/// </summary>
internal static class CopyCatJsonValueConverters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates the converter used for persisted filter definitions.
    /// </summary>
    /// <returns>The configured filter definition converter.</returns>
    internal static ValueConverter<FilterSetDefinition, string> CreateFilterSetDefinitionConverter()
    {
        return Create(FilterSetDefinition.AllowAll);
    }

    /// <summary>
    /// Creates the converter used for persisted rewrite rule sets.
    /// </summary>
    /// <returns>The configured rewrite rule set converter.</returns>
    internal static ValueConverter<RewriteRuleSet, string> CreateRewriteRuleSetConverter()
    {
        return Create(() => new RewriteRuleSet());
    }

    private static ValueConverter<TValue, string> Create<TValue>(Func<TValue> fallbackFactory)
        where TValue : class
    {
        return new ValueConverter<TValue, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<TValue>(value, JsonOptions) ?? fallbackFactory());
    }
}
