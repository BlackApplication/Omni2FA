namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>Result of a verified attestation — the new credential's stored material.</summary>
public class WebAuthnRegisteredCredential {
    public required byte[] CredentialId { get; init; }

    /// <summary>COSE-encoded public key.</summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>Initial signature counter reported by the authenticator.</summary>
    public required uint SignCount { get; init; }

    /// <summary>Authenticator model id; null when the authenticator did not report one.</summary>
    public Guid? Aaguid { get; init; }
}
