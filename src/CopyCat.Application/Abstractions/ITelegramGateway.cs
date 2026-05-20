using CopyCat.Domain.Entities;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Application.Abstractions;

/// <summary>
/// Provides Telegram client operations for login, channel discovery, and message forwarding.
/// </summary>
public interface ITelegramGateway
{
    /// <summary>
    /// Starts the interactive phone-number login flow for the session.
    /// </summary>
    /// <param name="session">The Telegram session to authenticate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartLoginAsync(TelegramSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the QR-code login flow for the session.
    /// </summary>
    /// <param name="session">The Telegram session to authenticate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartQrLoginAsync(TelegramSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a Telegram verification code during the login flow.
    /// </summary>
    /// <param name="session">The session awaiting code verification.</param>
    /// <param name="code">The verification code received via SMS or Telegram.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitCodeAsync(TelegramSession session, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-sends the Telegram verification code for the in-progress login.
    /// </summary>
    /// <param name="session">The session for which to resend the code.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResendCodeAsync(TelegramSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits the two-factor authentication password for the session.
    /// </summary>
    /// <param name="session">The session awaiting 2FA.</param>
    /// <param name="password">The two-factor authentication password.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitPasswordAsync(TelegramSession session, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all channels accessible by the session.
    /// </summary>
    /// <param name="session">The connected session to use for discovery.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of discovered Telegram channels.</returns>
    Task<IReadOnlyList<TelegramChannel>> DiscoverChannelsAsync(
        TelegramSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new broadcast channel via the session.
    /// </summary>
    /// <param name="session">The connected session to use for channel creation.</param>
    /// <param name="title">The display title for the new channel.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="TelegramChannel"/>.</returns>
    Task<TelegramChannel> CreateTargetChannelAsync(
        TelegramSession session,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a batch of historical messages from a source channel.
    /// </summary>
    /// <param name="session">The connected session to use.</param>
    /// <param name="sourceChannel">The channel from which to backfill messages.</param>
    /// <param name="take">Maximum number of messages to retrieve.</param>
    /// <param name="beforeTelegramMessageId">If provided, only messages older than this ID are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The backfilled messages.</returns>
    Task<IReadOnlyList<StoredMessage>> BackfillMessagesAsync(
        TelegramSession session,
        TelegramChannel sourceChannel,
        int take,
        long? beforeTelegramMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forwards or re-posts a message from the source channel to the target channel.
    /// </summary>
    /// <param name="session">The connected session to use.</param>
    /// <param name="sourceChannel">The channel the message originates from.</param>
    /// <param name="targetChannel">The channel to forward the message to.</param>
    /// <param name="message">The stored message to forward.</param>
    /// <param name="forwardingMode">Controls how the message is forwarded.</param>
    /// <param name="rewriteResult">Optional rewrite result to apply to the message text.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The Telegram message ID of the forwarded message, or <c>null</c> for native forwards.</returns>
    Task<long?> ExecuteForwardingAsync(
        TelegramSession session,
        TelegramChannel sourceChannel,
        TelegramChannel targetChannel,
        StoredMessage message,
        Domain.Enums.ForwardingMode forwardingMode,
        RewriteResult? rewriteResult,
        CancellationToken cancellationToken = default);
}
