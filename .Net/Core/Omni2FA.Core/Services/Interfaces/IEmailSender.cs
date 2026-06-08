using Omni2FA.Core.Dtos;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Email transport boundary. The default adapter implementation sends via MailKit/SMTP; hosts with
/// their own email infrastructure register a custom implementation and Omni2FA uses it instead.
/// </summary>
public interface IEmailSender {
    /// <summary>Deliver a composed message.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
