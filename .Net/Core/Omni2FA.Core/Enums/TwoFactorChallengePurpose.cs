namespace Omni2FA.Core.Enums;

/// <summary>
/// Why a <see cref="Entities.TwoFactorChallenge"/> exists. A challenge is consumed (or expires)
/// after one successful use; it bridges issuing state and verifying state.
/// </summary>
public enum TwoFactorChallengePurpose {
    /// <summary>Verifying a 2FA method during login (holds Email OTP hash or WebAuthn challenge bytes).</summary>
    Verify = 0,
    /// <summary>Pending TOTP enrollment — candidate secret stored until user confirms with a code.</summary>
    EnrollTotp = 1,
    /// <summary>Pending Email enrollment — server-issued OTP awaiting confirmation.</summary>
    EnrollEmail = 2,
    /// <summary>Pending WebAuthn enrollment — server challenge bytes awaiting attestation.</summary>
    EnrollWebAuthn = 3,
}
