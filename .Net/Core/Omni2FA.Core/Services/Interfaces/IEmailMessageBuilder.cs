using Omni2FA.Core.Dtos;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Composes the OTP email (subject + bodies) from a code and its lifetime. Separated from
/// <see cref="IEmailSender"/> so hosts can localize copy without replacing the transport.
/// </summary>
public interface IEmailMessageBuilder {
    /// <summary>Build the message for a freshly issued code addressed to <paramref name="recipient"/>.</summary>
    EmailMessage BuildOtpMessage(string recipient, string code, TimeSpan validFor);
}
