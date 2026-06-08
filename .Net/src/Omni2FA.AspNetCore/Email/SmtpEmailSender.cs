using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Email;

/// <summary>
/// Default <see cref="IEmailSender"/> built on MailKit/SMTP. Registered via <c>TryAdd</c> so a host
/// that registers its own sender (SendGrid, SES, an internal relay) replaces this without conflict.
/// </summary>
public class SmtpEmailSender : IEmailSender {
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<Omni2FaOptions> options) {
        _options = options.Value.Email;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName ?? string.Empty, _options.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        }.ToMessageBody();

        var smtp = _options.Smtp;
        using var client = new SmtpClient();
        var secureOption = smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(smtp.Host, smtp.Port, secureOption, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(smtp.Username)) {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
        }
        await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
