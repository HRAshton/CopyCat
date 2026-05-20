using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a complete filter set definition.
/// </summary>
public sealed record FilterSetDefinition(
    string Name,
    MappingDefaultPolicy DefaultPolicy,
    FilterNode? Root)
{
    /// <summary>
    /// Creates a definition that allows every message.
    /// </summary>
    /// <returns>A filter definition named "Allow all" with the allow policy and no root condition.</returns>
    public static FilterSetDefinition AllowAll()
    {
        return new FilterSetDefinition("Allow all", MappingDefaultPolicy.Allow, null);
    }
}
