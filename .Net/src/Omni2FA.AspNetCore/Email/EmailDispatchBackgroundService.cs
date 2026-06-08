using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Email;

/// <summary>
/// Drains <see cref="EmailDispatchChannel"/> and sends each queued message via <see cref="IEmailSender"/>,
/// resolved in a fresh scope per message so a host-registered scoped sender works. A send failure is
/// logged and the worker continues — it must never crash the host on a transient SMTP error.
/// </summary>
public sealed partial class EmailDispatchBackgroundService : BackgroundService {
    private readonly EmailDispatchChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDispatchBackgroundService> _logger;

    public EmailDispatchBackgroundService(EmailDispatchChannel channel, IServiceScopeFactory scopeFactory, ILogger<EmailDispatchBackgroundService> logger) {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false)) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(message, stoppingToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
#pragma warning disable CA1031 // Background delivery must not crash the host on any single send failure.
            catch (Exception ex) {
                LogSendFailure(ex, message.To);
            }
#pragma warning restore CA1031
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Omni2FA failed to send a verification email to {Recipient}.")]
    private partial void LogSendFailure(Exception exception, string recipient);
}
