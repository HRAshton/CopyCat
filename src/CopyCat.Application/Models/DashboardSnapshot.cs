namespace CopyCat.Application.Models;

/// <summary>
/// Represents a dashboard snapshot.
/// </summary>
public sealed record DashboardSnapshot(
    int EnabledSessions,
    int UnhealthySessions,
    int ActiveSourceChannels,
    int ActiveMappings,
    int PendingJobs,
    int FailedJobs,
    IReadOnlyList<string> LatestErrors);
