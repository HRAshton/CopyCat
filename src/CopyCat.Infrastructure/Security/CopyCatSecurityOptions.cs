namespace CopyCat.Infrastructure.Security;

/// <summary>
/// Represents a public API type.
/// </summary>
public sealed class CopyCatSecurityOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Security";

    /// <summary>
    /// Gets or sets a value.
    /// </summary>
    public string ApplicationName { get; set; } = "CopyCat";
}
