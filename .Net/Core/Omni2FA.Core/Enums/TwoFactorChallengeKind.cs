namespace Omni2FA.Core.Enums;

/// <summary>
/// Kind of ceremony a <see cref="Entities.TwoFactorChallenge"/> represents — either a login
/// verification, or one of the per-method enrollment flows. Consumed (or expired) after one
/// successful use.
/// </summary>
public enum TwoFactorChallengeKind {
    /// <summary>Verifying an existing 2FA method during login (holds Email OTP hash or WebAuthn challenge bytes).</summary>
    Login = 0,
    /// <summary>Pending TOTP enrollment — candidate secret stored until user confirms with a code.</summary>
    EnrollTotp = 1,
    /// <summary>Pending Email enrollment — server-issued OTP awaiting confirmation.</summary>
    EnrollEmail = 2,
    /// <summary>Pending WebAuthn enrollment — server challenge bytes awaiting attestation.</summary>
    EnrollWebAuthn = 3,
}
