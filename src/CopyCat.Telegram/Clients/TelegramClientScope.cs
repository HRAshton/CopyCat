using CopyCat.Application.Abstractions;
using CopyCat.Domain.Entities;

using Microsoft.Extensions.Logging;

using WTelegram;

namespace CopyCat.Telegram.Clients;

internal sealed class TelegramClientScope : IAsyncDisposable
{
    private readonly string sessionPath;

    private readonly TelegramSession session;

    private readonly ISecretProtector secretProtector;

    private TelegramClientScope(
        Client client,
        string sessionPath,
        TelegramSession session,
        ISecretProtector secretProtector)
    {
        Client = client;
        this.sessionPath = sessionPath;
        this.session = session;
        this.secretProtector = secretProtector;
    }

    internal Client Client { get; }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        if (File.Exists(sessionPath))
        {
            byte[] raw = await File.ReadAllBytesAsync(sessionPath);
            session.SessionDataEncrypted = secretProtector.Protect(Convert.ToBase64String(raw));
            File.Delete(sessionPath);
        }
    }

    internal static async Task<TelegramClientScope> CreateAsync(
        TelegramSession session,
        ISecretProtector secretProtector,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "copycat-sessions");
        Directory.CreateDirectory(tempDir);
        string sessionPath = Path.Combine(tempDir, $"{session.Id:N}.session");
        string? unprotected = secretProtector.UnprotectNullable(session.SessionDataEncrypted);
        if (!string.IsNullOrWhiteSpace(unprotected))
        {
            byte[] sessionBytes = Convert.FromBase64String(unprotected);
            await File.WriteAllBytesAsync(sessionPath, sessionBytes, cancellationToken);
        }

        TelegramLoggingConfigurator.EnsureConfigured(logger);
        int apiId = int.Parse(secretProtector.Unprotect(session.ApiIdEncrypted!));
        string apiHash = secretProtector.Unprotect(session.ApiHashEncrypted!);
        string? phoneNumber = session.PendingPhoneNumber ??
                              secretProtector.UnprotectNullable(session.PhoneNumberEncrypted);

        string? Config(string what)
        {
            return what switch
            {
                "api_id" => apiId.ToString(),
                "api_hash" => apiHash,
                "session_pathname" => sessionPath,
                "phone_number" => phoneNumber,
                _ => null,
            };
        }

        Client client = new(Config);

        return new TelegramClientScope(client, sessionPath, session, secretProtector);
    }
}
