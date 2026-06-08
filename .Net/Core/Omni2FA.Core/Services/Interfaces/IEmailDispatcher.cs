using Omni2FA.Core.Dtos;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Decides how a composed message reaches the <see cref="IEmailSender"/> — inline (awaited) or via a
/// background queue. The default adapter enqueues so OTP endpoints return without waiting on SMTP;
/// delivery failures are logged out-of-band rather than surfaced to the caller.
/// </summary>
public interface IEmailDispatcher {
    /// <summary>Hand a composed message off for delivery — queued or inline, per the dispatcher's policy.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
