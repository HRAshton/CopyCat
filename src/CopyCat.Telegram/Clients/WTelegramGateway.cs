using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Rewriting;

using Microsoft.Extensions.Logging;

using TL;

using WTelegram;

namespace CopyCat.Telegram.Clients;

/// <summary>
/// Implements Telegram login, discovery, backfill, and forwarding operations on top of the WTelegram client.
/// </summary>
public sealed class WTelegramGateway(
    ISecretProtector secretProtector,
    ITelegramAuthTraceStore authTraceStore,
    ITelegramQrLoginStore qrLoginStore,
    ITelegramInteractiveLoginSink interactiveLoginSink,
    TelegramPendingLoginStore pendingLoginStore,
    ILogger<WTelegramGateway> logger) : ITelegramGateway
{
    private static readonly HashSet<string> CodeChallenges = new(StringComparer.OrdinalIgnoreCase)
    {
        "verification_code", "email_verification_code",
    };

    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> SessionLocks = new();

    /// <summary>
    /// Starts a phone-based Telegram login flow for the specified session.
    /// </summary>
    /// <param name="session">The Telegram session whose credentials and login state should be used.</param>
    /// <param name="cancellationToken">The cancellation token for the login startup work.</param>
    /// <returns>A task that completes when the login flow has been started and the next step has been persisted.</returns>
    public async Task StartLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
    {
        await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                qrLoginStore.Clear(session.Id);
                await pendingLoginStore.ClearAsync(session.Id);
                authTraceStore.Record(session.Id, "Starting Telegram login.");
                TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                try
                {
                    string phone = NormalizePhoneNumber(
                        session.PendingPhoneNumber ?? secretProtector.UnprotectNullable(session.PhoneNumberEncrypted));
                    using IDisposable traceScope =
                        TelegramLoggingConfigurator.BeginSessionTrace(session.Id, authTraceStore);
                    string? next = await scope.Client.Login(phone);
                    ApplyLoginProgress(session, scope.Client, next);
                    await PersistOrKeepPendingAsync(session, scope);
                }
                catch
                {
                    await scope.DisposeAsync();
                    throw;
                }
            });
    }

    /// <summary>
    /// Starts a QR-code Telegram login flow for the specified session.
    /// </summary>
    /// <param name="session">The Telegram session whose login state should be updated.</param>
    /// <param name="cancellationToken">The cancellation token for the login startup work.</param>
    /// <returns>A task that completes when QR login setup has finished and the background flow has been launched.</returns>
    public async Task StartQrLoginAsync(TelegramSession session, CancellationToken cancellationToken = default)
    {
        await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                qrLoginStore.Clear(session.Id);
                await pendingLoginStore.ClearAsync(session.Id);
                authTraceStore.Record(session.Id, "Starting Telegram QR login.");
                TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                using IDisposable traceScope = TelegramLoggingConfigurator.BeginSessionTrace(
                    session.Id,
                    authTraceStore);
                _ = Task.Run(
                    async () => await RunQrLoginAsync(session, scope),
                    CancellationToken.None);
            });
    }

    /// <summary>
    /// Submits a verification code for an in-progress Telegram login flow.
    /// </summary>
    /// <param name="session">The Telegram session that is waiting for a verification code.</param>
    /// <param name="code">The verification code supplied by the user.</param>
    /// <param name="cancellationToken">The cancellation token for the submission work.</param>
    /// <returns>A task that completes when the verification code has been processed and the next login step has been persisted.</returns>
    public async Task SubmitCodeAsync(
        TelegramSession session,
        string code,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                authTraceStore.Record(session.Id, "Submitting Telegram verification code.");
                TelegramClientScope scope = await TakePendingScopeAsync(session, cancellationToken);
                try
                {
                    using IDisposable traceScope =
                        TelegramLoggingConfigurator.BeginSessionTrace(session.Id, authTraceStore);
                    string? next = await scope.Client.Login(code);
                    ApplyLoginProgress(session, scope.Client, next);
                    await PersistOrKeepPendingAsync(session, scope);
                }
                catch
                {
                    await scope.DisposeAsync();
                    throw;
                }
            });
    }

    /// <summary>
    /// Restarts a phone-based login flow so Telegram sends a fresh verification code.
    /// </summary>
    /// <param name="session">The Telegram session whose login flow should be restarted.</param>
    /// <param name="cancellationToken">The cancellation token for the restart work.</param>
    /// <returns>A task that completes when a fresh code request has been issued and the new login state has been persisted.</returns>
    public async Task ResendCodeAsync(TelegramSession session, CancellationToken cancellationToken = default)
    {
        await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                qrLoginStore.Clear(session.Id);
                authTraceStore.Record(
                    session.Id,
                    "Restarting Telegram login to request a fresh verification code.");
                await pendingLoginStore.ClearAsync(session.Id);
                TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                try
                {
                    string phone = NormalizePhoneNumber(
                        session.PendingPhoneNumber
                        ?? secretProtector.UnprotectNullable(session.PhoneNumberEncrypted));
                    using IDisposable traceScope = TelegramLoggingConfigurator.BeginSessionTrace(
                        session.Id,
                        authTraceStore);
                    string? next = await scope.Client.Login(phone);
                    ApplyLoginProgress(session, scope.Client, next);
                    await PersistOrKeepPendingAsync(session, scope);
                }
                catch
                {
                    await scope.DisposeAsync();
                    throw;
                }
            });
    }

    /// <summary>
    /// Submits a two-factor authentication password for an in-progress Telegram login flow.
    /// </summary>
    /// <param name="session">The Telegram session that is waiting for a password challenge.</param>
    /// <param name="password">The two-factor authentication password supplied by the user.</param>
    /// <param name="cancellationToken">The cancellation token for the submission work.</param>
    /// <returns>A task that completes when the password has been processed and the next login state has been persisted.</returns>
    public async Task SubmitPasswordAsync(
        TelegramSession session,
        string password,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                authTraceStore.Record(session.Id, "Submitting Telegram 2FA password.");
                TelegramClientScope scope = await TakePendingScopeAsync(session, cancellationToken);
                try
                {
                    using IDisposable traceScope =
                        TelegramLoggingConfigurator.BeginSessionTrace(session.Id, authTraceStore);
                    string? next = await scope.Client.Login(password);
                    ApplyLoginProgress(session, scope.Client, next);
                    await PersistOrKeepPendingAsync(session, scope);
                }
                catch
                {
                    await scope.DisposeAsync();
                    throw;
                }
            });
    }

    /// <summary>
    /// Discovers the dialogs and channels visible to the connected Telegram session.
    /// </summary>
    /// <param name="session">The connected Telegram session to inspect.</param>
    /// <param name="cancellationToken">The cancellation token for the discovery work.</param>
    /// <returns>The channels discovered for the session.</returns>
    public async Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(
        TelegramSession session,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                await using TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                await EnsureConnectedAsync(scope.Client, session);
                Messages_Dialogs? dialogs = await scope.Client.Messages_GetAllDialogs();
                return TelegramChannelMapper.ExtractChannels(session, dialogs);
            });
    }

    /// <summary>
    /// Creates a new broadcast target channel in Telegram.
    /// </summary>
    /// <param name="session">The connected Telegram session that should create the channel.</param>
    /// <param name="title">The title to assign to the new target channel.</param>
    /// <param name="cancellationToken">The cancellation token for the create operation.</param>
    /// <returns>The created Telegram channel mapped into the domain model.</returns>
    public async Task<TelegramChannel> CreateTargetChannelAsync(
        TelegramSession session,
        string title,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                await using TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                await EnsureConnectedAsync(scope.Client, session);
                UpdatesBase updates = await scope.Client.Channels_CreateChannel(
                    title,
                    "Created by CopyCat",
                    broadcast: true,
                    megagroup: false);
                TelegramChannel created = TelegramChannelMapper.ExtractCreatedChannel(session, updates);
                created.IsTarget = true;
                return created;
            });
    }

    /// <summary>
    /// Loads a page of historical messages from a Telegram source channel.
    /// </summary>
    /// <param name="session">The connected Telegram session that should read history.</param>
    /// <param name="sourceChannel">The source channel whose messages should be backfilled.</param>
    /// <param name="take">The maximum number of messages to request.</param>
    /// <param name="beforeTelegramMessageId">An optional exclusive upper bound used to paginate backward through history.</param>
    /// <param name="cancellationToken">The cancellation token for the backfill work.</param>
    /// <returns>The mapped stored-message representations returned by Telegram.</returns>
    public async Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(
        TelegramSession session,
        TelegramChannel sourceChannel,
        int take,
        long? beforeTelegramMessageId = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                await using TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                await EnsureConnectedAsync(scope.Client, session);
                InputPeer peer = await TelegramPeerResolver.ResolvePeerAsync(
                    scope.Client,
                    sourceChannel,
                    cancellationToken);
                int offsetMessageId =
                    beforeTelegramMessageId.HasValue ? checked((int)beforeTelegramMessageId.Value) : 0;
                Messages_MessagesBase? history = await scope.Client.Messages_GetHistory(
                    peer,
                    offset_id: offsetMessageId,
                    limit: take);
                return history.Messages.OfType<Message>()
                    .Select(message => TelegramMessageMapper.MapMessage(session, sourceChannel, message)).ToList();
            });
    }

    /// <summary>
    /// Sends or forwards a stored message from the source channel into the target channel.
    /// </summary>
    /// <param name="session">The connected Telegram session that should perform the forwarding.</param>
    /// <param name="sourceChannel">The source channel that originally contained the message.</param>
    /// <param name="targetChannel">The target channel that should receive the message.</param>
    /// <param name="message">The stored message to send or forward.</param>
    /// <param name="forwardingMode">The forwarding mode that determines how the message should be delivered.</param>
    /// <param name="rewriteResult">The optional rewritten text/caption payload produced by the rewrite pipeline.</param>
    /// <param name="cancellationToken">The cancellation token for the forwarding work.</param>
    /// <returns>The Telegram identifier of a newly sent message when Telegram creates one directly; otherwise, <see langword="null"/> for native forwards.</returns>
    public async Task<long?> ExecuteForwardingAsync(
        TelegramSession session,
        TelegramChannel sourceChannel,
        TelegramChannel targetChannel,
        StoredMessage message,
        ForwardingMode forwardingMode,
        RewriteResult? rewriteResult,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithSessionLockAsync(
            session.Id,
            async () =>
            {
                await using TelegramClientScope scope = await TelegramClientScope.CreateAsync(
                    session,
                    secretProtector,
                    logger,
                    cancellationToken);
                await EnsureConnectedAsync(scope.Client, session);
                InputPeer fromPeer = await TelegramPeerResolver.ResolvePeerAsync(
                    scope.Client,
                    sourceChannel,
                    cancellationToken);
                InputPeer toPeer = await TelegramPeerResolver.ResolvePeerAsync(
                    scope.Client,
                    targetChannel,
                    cancellationToken);
                bool hasAttachments = message.Attachments.Count > 0;
                string text = rewriteResult?.Text ?? message.Text ?? message.Caption ?? string.Empty;

                return forwardingMode switch
                {
                    ForwardingMode.NativeForward => await ForwardAsync(
                        scope.Client,
                        fromPeer,
                        toPeer,
                        message.TelegramMessageId,
                        false,
                        false),
                    ForwardingMode.CopyAsIs => await ForwardAsync(
                        scope.Client,
                        fromPeer,
                        toPeer,
                        message.TelegramMessageId,
                        true,
                        false),
                    ForwardingMode.AttachmentsWithoutText when hasAttachments => await ForwardAsync(
                        scope.Client,
                        fromPeer,
                        toPeer,
                        message.TelegramMessageId,
                        true,
                        true),
                    ForwardingMode.AttachmentsWithoutText => throw new InvalidOperationException(
                        "Attachments-only forwarding cannot be used for a text-only message."),
                    ForwardingMode.TextOnly when string.IsNullOrWhiteSpace(text) => throw new InvalidOperationException(
                        "Text-only forwarding produced an empty message."),
                    _ => (await scope.Client.SendMessageAsync(toPeer, text)).id,
                };
            });
    }

    private static async Task ExecuteWithSessionLockAsync(Guid sessionId, Func<Task> action)
    {
        SemaphoreSlim semaphore = SessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<T> ExecuteWithSessionLockAsync<T>(Guid sessionId, Func<Task<T>> action)
    {
        SemaphoreSlim semaphore = SessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<long?> ForwardAsync(
        Client client,
        InputPeer fromPeer,
        InputPeer toPeer,
        long telegramMessageId,
        bool dropAuthor,
        bool dropMediaCaptions)
    {
        await client.ForwardMessagesAsync(
            fromPeer,
            [checked((int)telegramMessageId)],
            toPeer,
            drop_author: dropAuthor,
            drop_media_captions: dropMediaCaptions);
        return null;
    }

    private static TelegramSessionStatus GetStatusFromChallenge(string challenge)
    {
        if (CodeChallenges.Contains(challenge))
        {
            return TelegramSessionStatus.AwaitingCode;
        }

        if (string.Equals(challenge, "password", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramSessionStatus.AwaitingPassword;
        }

        throw new InvalidOperationException($"Telegram requested an unsupported login challenge: '{challenge}'.");
    }

    private static string NormalizePhoneNumber(string? phoneNumber)
    {
        string normalized = (phoneNumber ?? string.Empty).Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("A phone number is required to start Telegram login.");
        }

        return normalized;
    }

    private async Task EnsureConnectedAsync(Client client, TelegramSession session)
    {
        if (client.User is not null)
        {
            return;
        }

        User user = await client.LoginUserIfNeeded(reloginOnFailedResume: false);
        session.Status = TelegramSessionStatus.Connected;
        session.PendingChallenge = null;
        session.LastConnectedAt = DateTimeOffset.UtcNow;
        session.LastError = null;
        authTraceStore.Record(session.Id, $"Telegram session resumed for user {user.id}.");
    }

    private void ApplyLoginProgress(TelegramSession session, Client client, string? next)
    {
        if (client.User is not null || string.IsNullOrWhiteSpace(next))
        {
            authTraceStore.Record(session.Id, "Telegram login completed successfully.");
            session.ApplyLoginProgress(null, TelegramSessionStatus.Connected, DateTimeOffset.UtcNow);
            return;
        }

        authTraceStore.Record(session.Id, $"Telegram requested next step: {next}.");
        TelegramSessionStatus status = GetStatusFromChallenge(next);
        session.ApplyLoginProgress(next, status, connectedAt: null);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "pendingScope is either null (nothing to dispose) or immediately returned.")]
    private async Task<TelegramClientScope> TakePendingScopeAsync(
        TelegramSession session,
        CancellationToken cancellationToken)
    {
        pendingLoginStore.TryTake(session.Id, out TelegramClientScope? pendingScope);
        if (pendingScope is not null)
        {
            return pendingScope;
        }

        authTraceStore.Record(session.Id, "Pending login scope was missing; reopening session from stored state.");
        return await TelegramClientScope.CreateAsync(session, secretProtector, logger, cancellationToken);
    }

    private async Task PersistOrKeepPendingAsync(TelegramSession session, TelegramClientScope scope)
    {
        if (session.Status == TelegramSessionStatus.Connected)
        {
            qrLoginStore.Clear(session.Id);
            await scope.DisposeAsync();
            return;
        }

        await pendingLoginStore.ReplaceAsync(session.Id, scope);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "QR login runs in a fire-and-forget background task.")]
    private async Task RunQrLoginAsync(TelegramSession session, TelegramClientScope scope)
    {
        try
        {
            User user = await scope.Client.LoginWithQRCode(url =>
            {
                qrLoginStore.SetUrl(session.Id, url);
                authTraceStore.Record(session.Id, $"Telegram QR login URL updated: {url}");
            });
            await scope.DisposeAsync();
            qrLoginStore.Clear(session.Id);
            authTraceStore.Record(session.Id, $"Telegram QR login completed for user {user.id}.");
            await interactiveLoginSink.CompleteLoginAsync(
                session.Id,
                session.SessionDataEncrypted,
                DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            await scope.DisposeAsync();
            qrLoginStore.Clear(session.Id);
            authTraceStore.Record(session.Id, $"Telegram QR login failed: {exception.Message}");
            await interactiveLoginSink.FailLoginAsync(session.Id, exception.Message);
        }
    }
}
