using Omni2FA.Core.Dtos;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Composes the OTP email (subject + bodies) from a code and its lifetime. Separated from
/// <see cref="IEmailSender"/> so hosts can localize copy without replacing the transport.
/// </summary>
public interface IEmailMessageBuilder {
    /// <summary>
    /// Build the message for a freshly issued code addressed to <paramref name="recipient"/>. Async because
    /// composing localized copy usually means reading the recipient's language and display name from the
    /// host's own store — the call already sits inside an awaited path, so nothing blocks a thread for it.
    /// </summary>
    Task<EmailMessage> BuildOtpMessageAsync(string recipient, string code, TimeSpan validFor, CancellationToken cancellationToken = default);
}
