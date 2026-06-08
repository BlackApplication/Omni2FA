namespace Omni2FA.Core.Configuration;

/// <summary>
/// Email OTP settings — code shape, lifetimes, sender identity, and SMTP transport. Used by both
/// Email enrollment and Email login challenges.
/// </summary>
public class EmailOptions {
    /// <summary>Number of digits in the emailed one-time code. Standard is 6.</summary>
    public int OtpDigits { get; set; } = 6;

    /// <summary>How long an emailed code remains valid after issuance.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Minimum interval between sends for the same challenge — throttles resend abuse.</summary>
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When true (default), OTP emails are queued and sent on a background worker so the issuing
    /// endpoint returns without waiting on SMTP; delivery failures are logged, not returned to the
    /// caller. Set false to send inline (awaited) — slower responses, but SMTP errors surface.
    /// </summary>
    public bool BackgroundDelivery { get; set; } = true;

    /// <summary>From address on outgoing OTP emails.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>From display name on outgoing OTP emails. Falls back to the issuer when empty.</summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Subject line for OTP emails. <c>{code}</c> is replaced with the generated code. The default
    /// is English; hosts override for localization or register a custom <c>IEmailMessageBuilder</c>.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Your verification code";

    /// <summary>SMTP transport settings for the default MailKit sender.</summary>
    public SmtpOptions Smtp { get; set; } = new();
}
