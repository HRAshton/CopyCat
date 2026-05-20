using CopyCat.Domain.Enums;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents a request to create or update a mapping.
/// </summary>
public sealed record MappingUpsertRequest(
    Guid? Id,
    Guid SourceChannelId,
    Guid TargetChannelId,
    bool IsEnabled,
    MappingDefaultPolicy DefaultPolicy,
    ForwardingMode ForwardingMode,
    Guid? ActiveFilterSetId,
    Guid? ActiveRewriteSetId,
    bool LiveForwardingEnabled,
    int BackfillCount);
