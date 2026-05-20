using CopyCat.Domain.Enums;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a summary of a Telegram session.
/// </summary>
public sealed record SessionSummary(
    Guid Id,
    string Name,
    string? PhoneNumberMasked,
    TelegramSessionStatus Status,
    bool IsEnabled,
    DateTimeOffset? LastConnectedAt,
    string? LastError,
    string? PendingChallenge,
    string? AuthTrace,
    string? QrLoginUrl);
