using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents the inline filter configuration for a mapping.
/// </summary>
public sealed record MappingFilterEditorModel(
    bool IsEnabled,
    MappingDefaultPolicy DefaultPolicy,
    FilterSetDefinition Definition);
