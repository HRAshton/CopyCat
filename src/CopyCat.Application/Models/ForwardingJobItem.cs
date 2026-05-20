using CopyCat.Domain.Enums;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a forwarding job item.
/// </summary>
public sealed record ForwardingJobItem(
    Guid Id,
    string SourceChannelTitle,
    string TargetChannelTitle,
    ForwardingJobStatus Status,
    ForwardingMode ForwardingMode,
    int Attempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextRetryAt);
