namespace CopyCat.Application.Models;

/// <summary>
/// Represents a request to create a Telegram session.
/// </summary>
public sealed record SessionCreateRequest(
    string Name,
    string PhoneNumber,
    string ApiId,
    string ApiHash);
