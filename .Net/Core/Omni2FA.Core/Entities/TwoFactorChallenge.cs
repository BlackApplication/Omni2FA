using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Entities;

/// <summary>
/// Short-lived server-side state for a 2FA ceremony in progress. One row per active ceremony.
/// Consumed (set <see cref="ConsumedAt"/>) on first successful verification, or pruned when
/// <see cref="ExpiresAt"/> passes.
/// </summary>
public class TwoFactorChallenge {
    /// <summary>Identifier of this challenge row.</summary>
    public Guid Id { get; set; }

    /// <summary>The host's user id this challenge belongs to.</summary>
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

    /// <summary>When this challenge was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this challenge stops being valid (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When this challenge was consumed by a successful verify. Null = still active. UTC.</summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>Count of failed verify attempts against this challenge.</summary>
    public int FailedAttempts { get; set; }
}
