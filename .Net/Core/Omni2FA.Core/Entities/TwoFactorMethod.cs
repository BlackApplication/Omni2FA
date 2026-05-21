using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Entities;

/// <summary>
/// A 2FA factor enrolled on a user. One row per method per user — a user can have multiple
/// methods of different types (TOTP + Email + several WebAuthn credentials) simultaneously
/// and pick one at login time.
/// </summary>
public class TwoFactorMethod {
    public Guid Id { get; set; }

    /// <summary>Host's user id. Omni2FA does not own the user table.</summary>
    public Guid UserId { get; set; }

    public TwoFactorMethodType Type { get; set; }

    /// <summary>Optional label ("Personal authenticator", "YubiKey 5"). Falls back to the type name in the UI when null.</summary>
    public string? Name { get; set; }

    /// <summary>Soft-disable flag. Inactive methods are not offered at login and are hidden in the UI.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC. Null until first use.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Protected base32 TOTP secret. Populated when <see cref="Type"/> is TOTP.</summary>
    public string? TotpSecret { get; set; }

    public byte[]? WebAuthnCredentialId { get; set; }

    /// <summary>COSE-encoded.</summary>
    public byte[]? WebAuthnPublicKey { get; set; }

    /// <summary>Signature counter from the authenticator. Used to detect cloned credentials.</summary>
    public uint? WebAuthnSignCount { get; set; }

    /// <summary>Identifies the authenticator model (e.g. YubiKey 5C NFC).</summary>
    public Guid? WebAuthnAaguid { get; set; }
}
