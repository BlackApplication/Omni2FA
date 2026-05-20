using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Entities;

/// <summary>
/// A 2FA factor enrolled on a user. One row per method per user — a user can have multiple
/// methods of different kinds (TOTP + Email + several WebAuthn credentials) simultaneously
/// and pick one at login time.
/// </summary>
public class TwoFactorMethod {
    /// <summary>Identifier of this method row.</summary>
    public Guid Id { get; set; }

    /// <summary>The host's user id this method belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>What kind of factor this is.</summary>
    public TwoFactorMethodKind Kind { get; set; }

    /// <summary>
    /// Optional human-readable label shown in the methods list ("Personal authenticator",
    /// "YubiKey 5", "Work phone"). Falls back to the kind name in the UI when null.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>Soft-disable flag. Inactive methods are not offered at login and are hidden in the UI.</summary>
    public bool IsActive { get; set; }

    /// <summary>When the user enrolled this method (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the method was most recently used to satisfy a login challenge (UTC). Null until first use.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Protected base32 TOTP secret. Populated when <see cref="Kind"/> is TOTP.</summary>
    public string? TotpSecret { get; set; }

    /// <summary>WebAuthn credential id (binary).</summary>
    public byte[]? WebAuthnCredentialId { get; set; }

    /// <summary>WebAuthn credential public key (COSE-encoded binary).</summary>
    public byte[]? WebAuthnPublicKey { get; set; }

    /// <summary>WebAuthn signature counter from the authenticator. Used to detect cloned credentials.</summary>
    public uint? WebAuthnSignCount { get; set; }

    /// <summary>WebAuthn AAGUID — identifies the authenticator model.</summary>
    public Guid? WebAuthnAaguid { get; set; }
}
