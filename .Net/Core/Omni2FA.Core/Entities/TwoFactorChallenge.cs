using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Entities;

/// <summary>
/// Short-lived server-side state for a 2FA ceremony in progress. One row per active ceremony.
/// Consumed (set <see cref="ConsumedAt"/>) on first successful verification, or pruned when
/// <see cref="ExpiresAt"/> passes.
/// </summary>
public class TwoFactorChallenge {
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Whether this challenge is for a login verification or for enrolling a new method,
    /// and (for enrollment) which method kind. Determines which of the kind-specific
    /// fields below is populated.
    /// </summary>
    public TwoFactorChallengePurpose Purpose { get; set; }

    /// <summary>
    /// The method this challenge targets at login time. Null for enrollment ceremonies
    /// (the method does not exist yet) and null right after password verification
    /// while the user is still picking a method.
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>Protected base32 TOTP secret candidate, awaiting confirmation by the user.</summary>
    public string? TotpSecretCandidate { get; set; }

    /// <summary>Hash of the server-issued Email OTP. Plaintext code is never stored.</summary>
    public string? EmailOtpHash { get; set; }

    /// <summary>Binary challenge bytes for a WebAuthn ceremony (registration or assertion).</summary>
    public byte[]? WebAuthnChallenge { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC. Null = still active.</summary>
    public DateTime? ConsumedAt { get; set; }

    public int FailedAttempts { get; set; }
}
