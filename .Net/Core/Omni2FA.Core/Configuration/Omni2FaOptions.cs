namespace Omni2FA.Core.Configuration;

/// <summary>
/// Root options object for Omni2FA. Composed from nested option groups, each bound
/// from a separate <c>appsettings</c> section.
/// </summary>
public class Omni2FaOptions {
    /// <summary>Configuration name conventionally used by <c>IOptions</c> binding.</summary>
    public const string SectionName = "Omni2Fa";

    /// <summary>Data Protection settings — scope used to encrypt TOTP secrets.</summary>
    public DataProtectionOptions DataProtection { get; set; } = new();

    /// <summary>TOTP-specific settings — issuer name, secret length, code digits, tolerance.</summary>
    public TotpOptions Totp { get; set; } = new();

    /// <summary>Email OTP settings — code shape, lifetimes, sender identity, SMTP transport.</summary>
    public EmailOptions Email { get; set; } = new();

    /// <summary>WebAuthn relying-party settings — RP id/name, allowed origins, per-user credential cap.</summary>
    public WebAuthnOptions WebAuthn { get; set; } = new();

    /// <summary>Pre-auth token settings — signing key, issuer, audience, TTL.</summary>
    public PreAuthOptions PreAuth { get; set; } = new();

    /// <summary>Settings consumed by the ASP.NET Core adapter — claim names, route prefix, enrollment TTL.</summary>
    public AspNetCoreOptions AspNetCore { get; set; } = new();
}
