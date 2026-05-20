using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Application.Services;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Tests;

public sealed class SessionServiceTests
{
    [Fact]
    public async Task GetSessionsAsync_OrdersByName_AndIncludesTraceState()
    {
        TelegramSession beta = new() { Name = "Beta", PhoneNumberMasked = "+2", Status = TelegramSessionStatus.Connected };
        TelegramSession alpha = new() { Name = "Alpha", PhoneNumberMasked = "+1", Status = TelegramSessionStatus.Pending };
        StubSessionStore store = new([beta, alpha]);
        StubAuthTraceStore auth = new();
        StubQrStore qr = new();
        auth.Record(alpha.Id, "trace-alpha");
        qr.SetUrl(alpha.Id, "qr-alpha");

        SessionService sut = CreateService(store, authTraceStore: auth, qrLoginStore: qr);

        IReadOnlyList<SessionSummary> result = await sut.GetSessionsAsync();

        Assert.Equal(["Alpha", "Beta"], result.Select(x => x.Name).ToArray());
        Assert.Equal("trace-alpha", result[0].AuthTrace);
        Assert.Equal("qr-alpha", result[0].QrLoginUrl);
    }

    [Fact]
    public async Task CreateSessionAsync_ProtectsSecrets_PersistsSession_AndAudits()
    {
        StubSessionStore store = new([]);
        StubSecretProtector protector = new();
        StubAuditLogService audit = new();
        SessionService sut = CreateService(store, secretProtector: protector, auditLogService: audit);

        SessionSummary created = await sut.CreateSessionAsync(
            new SessionCreateRequest("Primary", "+1234567890", "12345", "hash"));

        TelegramSession stored = Assert.Single(store.AddedSessions);
        Assert.Equal("Primary", stored.Name);
        Assert.Equal("masked:+1234567890", stored.PhoneNumberMasked);
        Assert.Equal("protected:+1234567890", stored.PhoneNumberEncrypted);
        Assert.Equal("protected:12345", stored.ApiIdEncrypted);
        Assert.Equal("protected:hash", stored.ApiHashEncrypted);
        Assert.Equal("protected:", stored.SessionDataEncrypted);
        Assert.Equal("+1234567890", stored.PendingPhoneNumber);
        Assert.Equal(stored.Id, created.Id);
        Assert.Equal("session.created", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task StartLoginAsync_OnSuccess_ResetsTransientState_AndStartsGateway()
    {
        TelegramSession session = new()
        {
            PendingChallenge = "password",
            LastError = "old",
            SessionDataEncrypted = "stale",
        };
        StubSessionStore store = new([session]);
        StubAuthTraceStore auth = new();
        StubQrStore qr = new();
        auth.Record(session.Id, "old trace");
        qr.SetUrl(session.Id, "old qr");
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway, auth, qr);

        await sut.StartLoginAsync(session.Id);

        Assert.Equal(1, gateway.StartLoginCalls);
        Assert.Equal("protected:", session.SessionDataEncrypted);
        Assert.Equal(TelegramSessionStatus.Pending, session.Status);
        Assert.Null(session.PendingChallenge);
        Assert.Null(session.LastError);
        Assert.Null(auth.GetLatest(session.Id));
        Assert.Null(qr.GetUrl(session.Id));
    }

    [Fact]
    public async Task StartLoginAsync_OnGatewayFailure_MarksSessionFaulted()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new() { StartLoginException = new InvalidOperationException("login failed") };
        SessionService sut = CreateService(store, gateway);

        await sut.StartLoginAsync(session.Id);

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("login failed", session.LastError);
        Assert.Equal(1, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartQrLoginAsync_OnFailure_MarksSessionFaulted()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new() { StartQrLoginException = new InvalidOperationException("QR failed") };
        SessionService sut = CreateService(store, gateway);

        await sut.StartQrLoginAsync(session.Id);

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("QR failed", session.LastError);
        Assert.Equal("qr_login", session.PendingChallenge);
        Assert.Equal(2, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartQrLoginAsync_OnSuccess_ClearsPreviousError_AndPersistsHandshakeState()
    {
        TelegramSession session = new() { LastError = "stale" };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway);

        await sut.StartQrLoginAsync(session.Id);

        Assert.Equal(1, gateway.StartQrLoginCalls);
        Assert.Equal(TelegramSessionStatus.Pending, session.Status);
        Assert.Equal("qr_login", session.PendingChallenge);
        Assert.Null(session.LastError);
        Assert.Equal(1, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task SubmitCodeAsync_WhenSessionDataIsInvalid_ResetsSessionAndStoresFriendlyFault()
    {
        TelegramSession session = new() { SessionDataEncrypted = "broken" };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new()
        {
            SubmitCodeException = new InvalidOperationException("Can't read session block from disk"),
        };
        SessionService sut = CreateService(store, gateway);

        await sut.SubmitCodeAsync(new LoginCodeRequest(session.Id, "12345"));

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Contains("Stored Telegram session data was invalid", session.LastError, StringComparison.Ordinal);
        Assert.Equal("protected:", session.SessionDataEncrypted);
    }

    [Fact]
    public async Task SubmitCodeAsync_OnSuccess_ClearsPreviousError()
    {
        TelegramSession session = new() { LastError = "bad code" };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway);

        await sut.SubmitCodeAsync(new LoginCodeRequest(session.Id, "12345"));

        Assert.Equal(1, gateway.SubmitCodeCalls);
        Assert.Null(session.LastError);
        Assert.Equal(1, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResendCodeAsync_OnSuccess_ClearsPreviousError()
    {
        TelegramSession session = new() { LastError = "expired" };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway);

        await sut.ResendCodeAsync(session.Id);

        Assert.Equal(1, gateway.ResendCodeCalls);
        Assert.Null(session.LastError);
        Assert.Equal(1, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResendCodeAsync_OnFailure_MarksSessionFaulted()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new() { ResendCodeException = new InvalidOperationException("code send failed") };
        SessionService sut = CreateService(store, gateway);

        await sut.ResendCodeAsync(session.Id);

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("code send failed", session.LastError);
    }

    [Fact]
    public async Task SubmitPasswordAsync_OnSuccess_ClearsPreviousError()
    {
        TelegramSession session = new() { LastError = "wrong password" };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway);

        await sut.SubmitPasswordAsync(new LoginPasswordRequest(session.Id, "secret"));

        Assert.Equal(1, gateway.SubmitPasswordCalls);
        Assert.Null(session.LastError);
        Assert.Equal(1, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task SubmitPasswordAsync_OnGenericFailure_MarksSessionFaulted()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new() { SubmitPasswordException = new InvalidOperationException("bad password") };
        SessionService sut = CreateService(store, gateway);

        await sut.SubmitPasswordAsync(new LoginPasswordRequest(session.Id, "secret"));

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("bad password", session.LastError);
    }

    [Fact]
    public async Task DisableSessionAsync_DisablesAndAudits()
    {
        TelegramSession session = new() { IsEnabled = true, Status = TelegramSessionStatus.Connected };
        StubSessionStore store = new([session]);
        StubAuditLogService audit = new();
        SessionService sut = CreateService(store, auditLogService: audit);

        await sut.DisableSessionAsync(session.Id);

        Assert.False(session.IsEnabled);
        Assert.Equal(TelegramSessionStatus.Disabled, session.Status);
        Assert.Equal("session.disabled", Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task ReconnectSessionAsync_EnablesAndRestartsLogin()
    {
        TelegramSession session = new() { IsEnabled = false, Status = TelegramSessionStatus.Disabled };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new();
        SessionService sut = CreateService(store, gateway);

        await sut.ReconnectSessionAsync(session.Id);

        Assert.True(session.IsEnabled);
        Assert.Equal(TelegramSessionStatus.Pending, session.Status);
        Assert.Equal(1, gateway.StartLoginCalls);
    }

    [Fact]
    public async Task ReconnectSessionAsync_OnGatewayFailure_LeavesSessionEnabledAndFaulted()
    {
        TelegramSession session = new() { IsEnabled = false, Status = TelegramSessionStatus.Disabled };
        StubSessionStore store = new([session]);
        StubTelegramGateway gateway = new() { StartLoginException = new InvalidOperationException("reconnect failed") };
        SessionService sut = CreateService(store, gateway);

        await sut.ReconnectSessionAsync(session.Id);

        Assert.True(session.IsEnabled);
        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("reconnect failed", session.LastError);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithDependents_Throws()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]) { HasDependents = true };
        SessionService sut = CreateService(store);

        InvalidDomainOperationException exception = await Assert.ThrowsAsync<InvalidDomainOperationException>(() =>
            sut.DeleteSessionAsync(session.Id));

        Assert.Contains("Only unused sessions can be deleted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithoutDependents_RemovesAndAudits()
    {
        TelegramSession session = new();
        StubSessionStore store = new([session]);
        StubAuditLogService audit = new();
        SessionService sut = CreateService(store, auditLogService: audit);

        await sut.DeleteSessionAsync(session.Id);

        Assert.Same(session, store.RemovedSession);
        Assert.Equal("session.deleted", Assert.Single(audit.Entries).Action);
    }

    private static SessionService CreateService(
        StubSessionStore sessionStore,
        StubTelegramGateway? telegramGateway = null,
        StubAuthTraceStore? authTraceStore = null,
        StubQrStore? qrLoginStore = null,
        StubSecretProtector? secretProtector = null,
        StubAuditLogService? auditLogService = null)
    {
        return new SessionService(
            sessionStore,
            telegramGateway ?? new StubTelegramGateway(),
            authTraceStore ?? new StubAuthTraceStore(),
            qrLoginStore ?? new StubQrStore(),
            secretProtector ?? new StubSecretProtector(),
            auditLogService ?? new StubAuditLogService());
    }

    private sealed class StubSessionStore(IReadOnlyList<TelegramSession> sessions) : ISessionStore
    {
        private readonly Dictionary<Guid, TelegramSession> sessionsById = sessions.ToDictionary(x => x.Id);

        public List<TelegramSession> AddedSessions { get; } = [];

        public bool HasDependents { get; init; }

        public TelegramSession? RemovedSession { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<TelegramSession>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TelegramSession>>(sessionsById.Values.ToList());
        }

        public Task<TelegramSession> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(sessionsById[sessionId]);
        }

        public Task AddAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            sessionsById[session.Id] = session;
            AddedSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<bool> HasDependentsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HasDependents);
        }

        public void Remove(TelegramSession session)
        {
            RemovedSession = session;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTelegramGateway : ITelegramGateway
    {
        public Exception? StartLoginException { get; init; }

        public Exception? StartQrLoginException { get; init; }

        public Exception? SubmitCodeException { get; init; }

        public Exception? ResendCodeException { get; init; }

        public Exception? SubmitPasswordException { get; init; }

        public int StartLoginCalls { get; private set; }

        public int StartQrLoginCalls { get; private set; }

        public int SubmitCodeCalls { get; private set; }

        public int ResendCodeCalls { get; private set; }

        public int SubmitPasswordCalls { get; private set; }

        public Task StartLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            StartLoginCalls++;
            if (StartLoginException is not null)
            {
                throw StartLoginException;
            }

            return Task.CompletedTask;
        }

        public Task StartQrLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            StartQrLoginCalls++;
            if (StartQrLoginException is not null)
            {
                throw StartQrLoginException;
            }

            return Task.CompletedTask;
        }

        public Task SubmitCodeAsync(TelegramSession session, string code, CancellationToken cancellationToken = default)
        {
            SubmitCodeCalls++;
            if (SubmitCodeException is not null)
            {
                throw SubmitCodeException;
            }

            return Task.CompletedTask;
        }

        public Task ResendCodeAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            ResendCodeCalls++;
            if (ResendCodeException is not null)
            {
                throw ResendCodeException;
            }

            return Task.CompletedTask;
        }

        public Task SubmitPasswordAsync(TelegramSession session, string password, CancellationToken cancellationToken = default)
        {
            SubmitPasswordCalls++;
            if (SubmitPasswordException is not null)
            {
                throw SubmitPasswordException;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(TelegramSession session, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TelegramChannel>>([]);
        }

        public Task<TelegramChannel> CreateTargetChannelAsync(TelegramSession session, string title, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TelegramChannel());
        }

        public Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(TelegramSession session, TelegramChannel sourceChannel, int take, long? before = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StoredMessage>>([]);
        }

        public Task<long?> ExecuteForwardingAsync(TelegramSession session, TelegramChannel sourceChannel, TelegramChannel targetChannel, StoredMessage message, ForwardingMode forwardingMode, CopyCat.Domain.Rewriting.RewriteResult? rewriteResult, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<long?>(null);
        }
    }

    private sealed class StubAuthTraceStore : ITelegramAuthTraceStore
    {
        private readonly Dictionary<Guid, string?> values = [];

        public string? GetLatest(Guid sessionId)
        {
            return values.GetValueOrDefault(sessionId);
        }

        public void Record(Guid sessionId, string traceLine)
        {
            values[sessionId] = traceLine;
        }

        public void Clear(Guid sessionId)
        {
            values.Remove(sessionId);
        }
    }

    private sealed class StubQrStore : ITelegramQrLoginStore
    {
        private readonly Dictionary<Guid, string?> values = [];

        public string? GetUrl(Guid sessionId)
        {
            return values.GetValueOrDefault(sessionId);
        }

        public void SetUrl(Guid sessionId, string url)
        {
            values[sessionId] = url;
        }

        public void Clear(Guid sessionId)
        {
            values.Remove(sessionId);
        }
    }

    private sealed class StubSecretProtector : ISecretProtector
    {
        public string Protect(string plainText)
        {
            return $"protected:{plainText}";
        }

        public string Unprotect(string protectedValue)
        {
            return protectedValue.Replace("protected:", string.Empty, StringComparison.Ordinal);
        }

        public string? ProtectNullable(string? plainText)
        {
            return plainText is null ? null : Protect(plainText);
        }

        public string? UnprotectNullable(string? protectedValue)
        {
            return protectedValue is null ? null : Unprotect(protectedValue);
        }

        public string MaskPhoneNumber(string phoneNumber)
        {
            return $"masked:{phoneNumber}";
        }
    }

    private sealed class StubAuditLogService : IAuditLogService
    {
        public List<(string Action, string EntityType, Guid? EntityId, object? Before, object? After)> Entries { get; } = [];

        public Task<IReadOnlyList<AuditLogItem>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AuditLogItem>>([]);
        }

        public Task WriteAsync(string action, string entityType, Guid? entityId, object? before, object? after, CancellationToken cancellationToken = default)
        {
            Entries.Add((action, entityType, entityId, before, after));
            return Task.CompletedTask;
        }
    }
}
