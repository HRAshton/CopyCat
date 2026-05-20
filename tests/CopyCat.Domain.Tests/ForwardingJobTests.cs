using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Tests;

public sealed class ForwardingJobTests
{
    [Fact]
    public void MarkProcessing_TransitionsJobToProcessing()
    {
        ForwardingJob job = CreateJob();

        job.MarkProcessing();

        Assert.Equal(ForwardingJobStatus.Processing, job.Status);
    }

    [Fact]
    public void RecordAttemptFailure_BeforeLimit_SchedulesRetry()
    {
        ForwardingJob job = CreateJob();

        job.RecordAttemptFailure("temporary", maxAttempts: 3);

        Assert.Equal(1, job.Attempts);
        Assert.Equal(ForwardingJobStatus.FailedTransient, job.Status);
        Assert.Equal("temporary", job.LastError);
        Assert.NotNull(job.NextRetryAt);
        Assert.True(job.NextRetryAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RecordAttemptFailure_AtLimit_FailsPermanently()
    {
        ForwardingJob job = CreateJob();
        job.Attempts = 2;

        job.RecordAttemptFailure("fatal", maxAttempts: 3);

        Assert.Equal(3, job.Attempts);
        Assert.Equal(ForwardingJobStatus.FailedPermanent, job.Status);
        Assert.Equal("fatal", job.LastError);
        Assert.Null(job.NextRetryAt);
    }

    private static ForwardingJob CreateJob()
    {
        return new ForwardingJob
        {
            MessageId = Guid.NewGuid(),
            MappingId = Guid.NewGuid(),
            FilterVersionId = Guid.NewGuid(),
            RewriteVersionId = Guid.NewGuid(),
            ForwardingMode = ForwardingMode.CopyWithRewriting,
            Attempts = 0,
            LastError = null,
            NextRetryAt = null,
            TargetTelegramMessageId = null,
        };
    }
}
