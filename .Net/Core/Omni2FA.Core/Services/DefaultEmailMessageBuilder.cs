using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>
/// Built-in English OTP email composer. Plain strings only — no framework dependency. Hosts needing
/// localization or richer markup register their own <see cref="IEmailMessageBuilder"/>.
/// </summary>
public class DefaultEmailMessageBuilder : IEmailMessageBuilder {
    private readonly EmailOptions _options;

    public DefaultEmailMessageBuilder(IOptions<Omni2FaOptions> options) {
        _options = options.Value.Email;
    }

    public EmailMessage BuildOtpMessage(string recipient, string code, TimeSpan validFor) {
        var minutes = Math.Max(1, (int)Math.Round(validFor.TotalMinutes));
        var subject = _options.SubjectTemplate.Replace("{code}", code, StringComparison.Ordinal);
        var text = string.Format(
            CultureInfo.InvariantCulture,
            "Your verification code is {0}. It is valid for {1} minutes. If you did not request this, you can safely ignore this email.",
            code, minutes);
        var html = string.Format(
            CultureInfo.InvariantCulture,
            "<p>Your verification code is:</p><p style=\"font-size:24px;font-weight:bold;letter-spacing:3px\">{0}</p><p>It is valid for {1} minutes. If you did not request this, you can safely ignore this email.</p>",
            WebUtility.HtmlEncode(code), minutes);
        return new EmailMessage {
            To = recipient,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
    }
}
