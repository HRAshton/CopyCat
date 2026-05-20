namespace CopyCat.Application.Models;

/// <summary>
/// Represents a login password submission.
/// </summary>
public sealed record LoginPasswordRequest(Guid SessionId, string Password);
