using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Models;
using CopyCat.Application.Options;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Messages;
using CopyCat.Domain.Rewriting;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CopyCat.Application.Tests;

public sealed class ApplicationForwardingProcessingServiceTests
{
    [Fact]
    public async Task ProcessBatchAsync_SuccessfulJob_MarksSucceededAsync()
    {
        ForwardingJob job = MakePendingJob();
        StubWorkStore store = new([job], MakeContext(job));
        StubGateway gateway = new(targetMessageId: 42L);
        StubRewriter rewriter = new();

        ApplicationForwardingProcessingService sut = CreateService(store, gateway, rewriter);
        await sut.ProcessBatchAsync();

        Assert.Equal(ForwardingJobStatus.Succeeded, job.Status);
        Assert.Equal(42L, job.TargetTelegramMessageId);
        Assert.Null(job.LastError);
        Assert.Null(job.NextRetryAt);
        Assert.Equal(2, store.SaveCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_FailingJob_MarksTransientAndSchedulesRetryAsync()
    {
        ForwardingJob job = MakePendingJob();
        StubWorkStore store = new([job], MakeContext(job));
        StubGateway gateway = new(shouldThrow: true);
        StubRewriter rewriter = new();

        ApplicationForwardingProcessingService sut = CreateService(store, gateway, rewriter);
        await sut.ProcessBatchAsync();

        Assert.Equal(ForwardingJobStatus.FailedTransient, job.Status);
        Assert.Equal(1, job.Attempts);
        Assert.Equal("Gateway error", job.LastError);
        Assert.NotNull(job.NextRetryAt);
        Assert.True(job.NextRetryAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProcessBatchAsync_RepeatedFailures_MarksPermanentlyFailedAsync()
    {
        ForwardingJob job = MakePendingJob();
        StubWorkStore store = new([job], MakeContext(job));
        StubGateway gateway = new(shouldThrow: true);
        StubRewriter rewriter = new();
        ApplicationForwardingProcessingService sut = CreateService(store, gateway, rewriter);

        for (int pass = 0; pass < 5; pass++)
        {
            store.Reset([job]);
            await sut.ProcessBatchAsync();
        }

        Assert.Equal(ForwardingJobStatus.FailedPermanent, job.Status);
        Assert.Equal(5, job.Attempts);
        Assert.Null(job.NextRetryAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_EmptyBatch_DoesNothingAsync()
    {
        StubWorkStore store = new([], null);
        StubGateway gateway = new();
        StubRewriter rewriter = new();

        ApplicationForwardingProcessingService sut = CreateService(store, gateway, rewriter);
        await sut.ProcessBatchAsync();

        Assert.Equal(0, store.SaveCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenRewriteVersionExists_RewritesBeforeForwardingAsync()
    {
        ForwardingJob job = MakePendingJob();
        RewriteVersion rewriteVersion = new()
        {
            Rules = new RewriteRuleSet([new AppendFooterOperation("Footer")]),
        };
        StubGateway gateway = new(targetMessageId: 77L);
        StubRewriter rewriter = new();
        StubWorkStore store = new([job], MakeContext(job, rewriteVersion));

        ApplicationForwardingProcessingService sut = CreateService(store, gateway, rewriter);
        await sut.ProcessBatchAsync();

        Assert.Equal(1, rewriter.CallCount);
        Assert.NotNull(gateway.LastRewriteResult);
        Assert.Equal(["passthrough"], gateway.LastRewriteResult!.Trace);
        Assert.Equal(77L, job.TargetTelegramMessageId);
    }

    private static ApplicationForwardingProcessingService CreateService(
        IForwardingWorkStore store,
        ITelegramGateway gateway,
        IMessageRewriter rewriter)
    {
        IOptions<ApplicationWorkerOptions> opts =
            Microsoft.Extensions.Options.Options.Create(new ApplicationWorkerOptions());
        return new ApplicationForwardingProcessingService(
            store,
            gateway,
            rewriter,
            opts,
            NullLogger<ApplicationForwardingProcessingService>.Instance);
    }

    private static ForwardingJob MakePendingJob()
    {
        return new ForwardingJob
        {
            MessageId = Guid.NewGuid(),
            MappingId = Guid.NewGuid(),
            ForwardingMode = ForwardingMode.CopyWithRewriting,
        };
    }

    private static ForwardingExecutionContext MakeContext(ForwardingJob job, RewriteVersion? rewriteVersion = null)
    {
        return new ForwardingExecutionContext(
            job,
            new ChannelMapping(),
            new TelegramChannel { Title = "Source" },
            new TelegramChannel { Title = "Target" },
            new TelegramSession { Name = "Session" },
            new StoredMessage(),
            RewriteVersion: rewriteVersion);
    }

    private sealed class StubWorkStore(
        IReadOnlyList<ForwardingJob> jobs,
        ForwardingExecutionContext? context) : IForwardingWorkStore
    {
        private IReadOnlyList<ForwardingJob> jobs = jobs;

        public int SaveCallCount { get; private set; }

        public void Reset(IReadOnlyList<ForwardingJob> newJobs)
        {
            jobs = newJobs;
        }

        public Task<IReadOnlyList<ForwardingJob>> GetReadyJobsAsync(
            int take,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(jobs);
        }

        public Task<ForwardingExecutionContext> GetExecutionContextAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context!);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubGateway(long? targetMessageId = null, bool shouldThrow = false) : ITelegramGateway
    {
        public RewriteResult? LastRewriteResult { get; private set; }

        public Task<long?> ExecuteForwardingAsync(
            TelegramSession session,
            TelegramChannel sourceChannel,
            TelegramChannel targetChannel,
            StoredMessage message,
            ForwardingMode forwardingMode,
            RewriteResult? rewriteResult,
            CancellationToken cancellationToken = default)
        {
            if (shouldThrow)
            {
                throw new InvalidOperationException("Gateway error");
            }

            LastRewriteResult = rewriteResult;
            return Task.FromResult(targetMessageId);
        }

        public Task StartLoginAsync(TelegramSession s, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task StartQrLoginAsync(TelegramSession s, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task SubmitCodeAsync(TelegramSession s, string c, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task ResendCodeAsync(TelegramSession s, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task SubmitPasswordAsync(TelegramSession s, string p, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(
            TelegramSession s,
            CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<TelegramChannel>>([]);
        }

        public Task<TelegramChannel> CreateTargetChannelAsync(
            TelegramSession s,
            string t,
            CancellationToken ct = default)
        {
            return Task.FromResult(new TelegramChannel());
        }

        public Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(
            TelegramSession s,
            TelegramChannel ch,
            int take,
            long? before = null,
            CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<StoredMessage>>([]);
        }
    }

    private sealed class StubRewriter : IMessageRewriter
    {
        public int CallCount { get; private set; }

        public RewriteResult Rewrite(NormalizedTelegramMessage message, RewriteRuleSet rules)
        {
            CallCount++;
            return new RewriteResult(null, null, false, ["passthrough"]);
        }
    }
}
