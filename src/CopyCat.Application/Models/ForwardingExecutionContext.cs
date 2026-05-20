using CopyCat.Domain.Entities;

namespace CopyCat.Application.Models;

/// <summary>
/// Represents all loaded state needed to execute a forwarding job.
/// </summary>
public sealed record ForwardingExecutionContext(
    ForwardingJob Job,
    ChannelMapping Mapping,
    TelegramChannel SourceChannel,
    TelegramChannel TargetChannel,
    TelegramSession Session,
    StoredMessage Message,
    RewriteVersion? RewriteVersion);
