using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Email;

/// <summary>
/// Default <see cref="IEmailDispatcher"/>. Honors <see cref="EmailOptions.BackgroundDelivery"/>: when
/// on, enqueues onto <see cref="EmailDispatchChannel"/> (instant return, background SMTP); when off,
/// sends inline via <see cref="IEmailSender"/> (awaited, SMTP errors surface to the caller).
/// </summary>
public sealed class EmailDispatcher : IEmailDispatcher {
    private readonly EmailDispatchChannel _channel;
    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;

    public EmailDispatcher(EmailDispatchChannel channel, IEmailSender sender, IOptions<Omni2FaOptions> options) {
        _channel = channel;
        _sender = sender;
        _options = options.Value.Email;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        if (_options.BackgroundDelivery) {
            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        } else {
            await _sender.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
