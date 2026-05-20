namespace CopyCat.Application.Models;

/// <summary>
/// Represents a login code submission.
/// </summary>
public sealed record LoginCodeRequest(Guid SessionId, string Code);
