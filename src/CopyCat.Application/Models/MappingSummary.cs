using CopyCat.Domain.Enums;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a summary of a mapping.
/// </summary>
public sealed record MappingSummary(
    Guid Id,
    Guid SourceChannelId,
    string SourceChannelTitle,
    Guid TargetChannelId,
    string TargetChannelTitle,
    bool IsEnabled,
    ForwardingMode ForwardingMode,
    bool LiveForwardingEnabled,
    bool HasFilter,
    bool HasRewrite,
    int BackfillCount);
