using System.Globalization;
using System.Net;
using System.Text;
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
    private const string Heading = "Your verification code";
    private const string Note = "If you did not request this code, you can safely ignore this email.";
    private const string AutomatedNotice = "This is an automated message — please do not reply to it.";

    // Table layout with inline styles only: Outlook ignores <style> blocks and Gmail strips them.
    private static readonly CompositeFormat HtmlTemplate = CompositeFormat.Parse("""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{0}</title></head>
        <body style="margin:0;padding:0;background:#eef1f6;">
        <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">{1}</div>
        <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="background:#eef1f6;padding:32px 12px;">
          <tr><td align="center">
            <table width="600" cellpadding="0" cellspacing="0" border="0" role="presentation" style="max-width:600px;width:100%;background:#ffffff;border:1px solid #e4e8f0;border-radius:18px;overflow:hidden;">
              <tr><td style="height:4px;background:#075ee6;font-size:0;line-height:0;">&nbsp;</td></tr>
              <tr><td style="padding:40px 40px 0 40px;">
                <table cellpadding="0" cellspacing="0" border="0" role="presentation"><tr>
                  <td align="center" valign="middle" style="background:#e6efff;border-radius:12px;width:46px;height:46px;">
                    <span style="display:inline-block;width:46px;color:#075ee6;font-size:22px;line-height:46px;text-align:center;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;">&#128274;</span>
                  </td>
                  <td valign="middle" style="padding-left:16px;">
                    <h1 style="margin:0;font-size:21px;font-weight:700;color:#1f2430;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;line-height:1.3;letter-spacing:-0.2px;">{0}</h1>
                  </td>
                </tr></table>
              </td></tr>
              <tr><td style="padding:26px 40px 0 40px;">
                <p style="margin:0;font-size:15px;color:#4d5566;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;line-height:1.65;">{2}</p>
              </td></tr>
              <tr><td style="padding:20px 40px 0 40px;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="background:#f6f8fb;border-radius:14px;">
                  <tr><td align="center" style="padding:22px 16px;">
                    <div style="font-size:30px;font-weight:700;color:#1f2430;font-family:'SF Mono',Menlo,Consolas,'Courier New',monospace;letter-spacing:7px;">{3}</div>
                  </td></tr>
                </table>
              </td></tr>
              <tr><td style="padding:16px 40px 0 40px;">
                <p style="margin:0;font-size:15px;color:#4d5566;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;line-height:1.65;">{4}</p>
              </td></tr>
              <tr><td style="padding:34px 40px 0 40px;"><div style="height:1px;background:#edf0f5;font-size:0;line-height:0;">&nbsp;</div></td></tr>
              <tr><td align="center" style="padding:18px 40px 30px 40px;text-align:center;">
                <p style="margin:0;font-size:12px;color:#8b93a5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;line-height:1.6;text-align:center;">{5}</p>
                <p style="margin:8px 0 0 0;font-size:12px;color:#8b93a5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;text-align:center;">&copy; {6} {7}</p>
              </td></tr>
            </table>
          </td></tr>
        </table>
        </body>
        </html>
        """);

    private static readonly CompositeFormat TextTemplate = CompositeFormat.Parse("""
        {0}

        {1}

        {2}

        {3}

        {4}
        {5}
        """);

    private readonly EmailOptions _options;
    private readonly string _brandName;

    public DefaultEmailMessageBuilder(IOptions<Omni2FaOptions> options) {
        _options = options.Value.Email;
        _brandName = string.IsNullOrWhiteSpace(_options.FromName) ? options.Value.Totp.Issuer : _options.FromName;
    }

    public Task<EmailMessage> BuildOtpMessageAsync(string recipient, string code, TimeSpan validFor, CancellationToken cancellationToken = default) {
        var minutes = Math.Max(1, (int)Math.Round(validFor.TotalMinutes));
        var lifetime = minutes == 1 ? "1 minute" : string.Format(CultureInfo.InvariantCulture, "{0} minutes", minutes);
        var validity = string.Format(CultureInfo.InvariantCulture, "It stays valid for {0}.", lifetime);
        var intro = "Enter the code below to continue. " + validity;
        var preheader = string.Format(CultureInfo.InvariantCulture, "Your code expires in {0}.", lifetime);

        var subject = _options.SubjectTemplate.Replace("{code}", code, StringComparison.Ordinal);
        var html = string.Format(
            CultureInfo.InvariantCulture,
            HtmlTemplate,
            Heading,
            preheader,
            intro,
            WebUtility.HtmlEncode(code),
            Note,
            AutomatedNotice,
            DateTime.UtcNow.Year,
            WebUtility.HtmlEncode(_brandName));
        var text = string.Format(CultureInfo.InvariantCulture, TextTemplate, Heading + ":", code, validity, Note, _brandName, AutomatedNotice);

        var message = new EmailMessage {
            To = recipient,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
        return Task.FromResult(message);
    }
}
