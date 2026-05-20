using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Application.Exceptions;
using CopyCat.Application.Models;
using CopyCat.Domain.Entities;

namespace CopyCat.Application.Services;

/// <summary>
/// Manages Telegram session lifecycle, authentication, and connection.
/// </summary>
internal sealed class SessionService(
    ISessionStore sessionStore,
    ITelegramGateway telegramGateway,
    ITelegramAuthTraceStore authTraceStore,
    ITelegramQrLoginStore qrLoginStore,
    ISecretProtector secretProtector,
    IAuditLogService auditLogService) : ISessionManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TelegramSession> sessions = await sessionStore.ListAsync(cancellationToken);
        return sessions
            .OrderBy(x => x.Name)
            .Select(ToSummary)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<SessionSummary> CreateSessionAsync(
        SessionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        TelegramSession session = new()
        {
            Name = request.Name,
            PhoneNumberMasked = secretProtector.MaskPhoneNumber(request.PhoneNumber),
            PhoneNumberEncrypted = secretProtector.Protect(request.PhoneNumber),
            ApiIdEncrypted = secretProtector.Protect(request.ApiId),
            ApiHashEncrypted = secretProtector.Protect(request.ApiHash),
            SessionDataEncrypted = secretProtector.Protect(string.Empty),
            PendingPhoneNumber = request.PhoneNumber,
        };

        await sessionStore.AddAsync(session, cancellationToken);
        await sessionStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "session.created",
            nameof(TelegramSession),
            session.Id,
            null,
            session,
            cancellationToken);
        return ToSummary(session);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task StartLoginAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        try
        {
            ResetSessionForFreshLogin(session);
            await telegramGateway.StartLoginAsync(session, cancellationToken);
            session.ClearError();
        }
        catch (Exception exception)
        {
            session.MarkFaulted(exception.Message);
        }

        await sessionStore.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task StartQrLoginAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        try
        {
            ResetSessionForFreshLogin(session);
            session.PendingChallenge = "qr_login";
            await sessionStore.SaveChangesAsync(cancellationToken);
            await telegramGateway.StartQrLoginAsync(session, cancellationToken);
            session.ClearError();
        }
        catch (Exception exception)
        {
            session.MarkFaulted(exception.Message);
            await sessionStore.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task SubmitCodeAsync(LoginCodeRequest request, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(request.SessionId, cancellationToken);
        try
        {
            await telegramGateway.SubmitCodeAsync(session, request.Code, cancellationToken);
            session.ClearError();
        }
        catch (Exception exception)
        {
            HandleLoginException(session, exception);
        }

        await sessionStore.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task ResendCodeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        try
        {
            await telegramGateway.ResendCodeAsync(session, cancellationToken);
            session.ClearError();
        }
        catch (Exception exception)
        {
            session.MarkFaulted(exception.Message);
        }

        await sessionStore.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task SubmitPasswordAsync(LoginPasswordRequest request, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(request.SessionId, cancellationToken);
        try
        {
            await telegramGateway.SubmitPasswordAsync(session, request.Password, cancellationToken);
            session.ClearError();
        }
        catch (Exception exception)
        {
            HandleLoginException(session, exception);
        }

        await sessionStore.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DisableSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        session.Disable();
        await sessionStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "session.disabled",
            nameof(TelegramSession),
            session.Id,
            null,
            session,
            cancellationToken);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maps Telegram failures into durable session state.")]
    public async Task ReconnectSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        session.Enable();
        try
        {
            ResetSessionForFreshLogin(session);
            await telegramGateway.StartLoginAsync(session, cancellationToken);
        }
        catch (Exception exception)
        {
            session.MarkFaulted(exception.Message);
        }

        await sessionStore.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        TelegramSession session = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (await sessionStore.HasDependentsAsync(sessionId, cancellationToken))
        {
            throw new InvalidDomainOperationException(
                "Only unused sessions can be deleted. Remove dependent channels and message history first.");
        }

        sessionStore.Remove(session);
        await sessionStore.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "session.deleted",
            nameof(TelegramSession),
            sessionId,
            null,
            new { Id = sessionId },
            cancellationToken);
    }

    private SessionSummary ToSummary(TelegramSession session)
    {
        return new SessionSummary(
            session.Id,
            session.Name,
            session.PhoneNumberMasked,
            session.Status,
            session.IsEnabled,
            session.LastConnectedAt,
            session.LastError,
            session.PendingChallenge,
            authTraceStore.GetLatest(session.Id),
            qrLoginStore.GetUrl(session.Id));
    }

    private void ResetSessionForFreshLogin(TelegramSession session)
    {
        authTraceStore.Clear(session.Id);
        qrLoginStore.Clear(session.Id);
        session.ResetForFreshLogin(secretProtector.Protect(string.Empty));
    }

    private void HandleLoginException(TelegramSession session, Exception exception)
    {
        if (exception.Message.Contains("Can't read session block", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("delete the file to start a new session", StringComparison.OrdinalIgnoreCase))
        {
            ResetSessionForFreshLogin(session);
            session.MarkFaulted(
                "Stored Telegram session data was invalid for the supplied API credentials. The local session was reset. Click Login again to start a fresh Telegram login.");
        }
        else
        {
            session.MarkFaulted(exception.Message);
        }
    }
}
