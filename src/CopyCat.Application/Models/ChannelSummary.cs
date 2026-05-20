using CopyCat.Domain.Enums;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a summary of a discovered Telegram channel.
/// </summary>
public sealed record ChannelSummary(
    Guid Id,
    Guid SessionId,
    string Title,
    string? Username,
    TelegramChannelType ChannelType,
    bool IsSource,
    bool IsTarget,
    bool? CanPost,
    bool? CanAdmin,
    DateTimeOffset DiscoveredAt);
