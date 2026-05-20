using CopyCat.Domain.Filters;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a filter set editor model.
/// </summary>
public sealed record FilterSetEditorModel(
    Guid? FilterSetId,
    string Name,
    string? Description,
    FilterSetDefinition Definition);
