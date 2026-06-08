namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>The stored credential material an assertion is verified against.</summary>
public class WebAuthnStoredCredential {
    public required byte[] CredentialId { get; init; }

    /// <summary>COSE-encoded public key.</summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>Last signature counter seen for this credential — used to detect cloned authenticators.</summary>
    public required uint SignCount { get; init; }
}
