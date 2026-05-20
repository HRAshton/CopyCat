namespace CopyCat.Application.Models;

/// <summary>
/// Payload for the <c>RunBackfill</c> control operation.
/// </summary>
public sealed record RunBackfillPayload(int Take);
