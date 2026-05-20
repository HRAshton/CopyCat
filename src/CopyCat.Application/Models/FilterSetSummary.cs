using CopyCat.Domain.Filters;

namespace CopyCat.Application.Models;

/// <summary>
/// A read-only summary of a filter set returned to callers outside the Application layer.
/// </summary>
public sealed record FilterSetSummary(
    Guid Id,
    string Name,
    string? Description,
    int VersionCount,
    FilterSetDefinition? LatestDefinition);
