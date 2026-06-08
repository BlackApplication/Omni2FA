namespace Omni2FA.Core.Configuration;

/// <summary>
/// SMTP transport settings consumed by the default MailKit-based <c>IEmailSender</c>. Hosts with
/// their own email infrastructure register a custom <c>IEmailSender</c> and can ignore this section.
/// </summary>
public class SmtpOptions {
    /// <summary>SMTP server host name.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port. 587 for STARTTLS, 465 for implicit TLS, 25 for unencrypted relays.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Account user name. Leave empty for anonymous local relays / dev catchers.</summary>
    public string? Username { get; set; }

    /// <summary>Account password. Provide via configuration or environment — never hard-code.</summary>
    public string? Password { get; set; }

    /// <summary>Use STARTTLS to upgrade the connection. Disable only for local dev catchers.</summary>
    public bool UseStartTls { get; set; } = true;
}
