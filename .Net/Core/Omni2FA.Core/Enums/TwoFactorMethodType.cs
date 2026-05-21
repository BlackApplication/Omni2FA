namespace Omni2FA.Core.Enums;

/// <summary>The type of an enrolled 2FA factor.</summary>
public enum TwoFactorMethodType {
    /// <summary>Time-based one-time password — authenticator apps (Google Authenticator, Authy, 1Password, …).</summary>
    Totp = 0,
    /// <summary>Server-issued OTP delivered by email.</summary>
    Email = 1,
    /// <summary>WebAuthn — passkeys and FIDO2 security keys.</summary>
    WebAuthn = 2,
}
