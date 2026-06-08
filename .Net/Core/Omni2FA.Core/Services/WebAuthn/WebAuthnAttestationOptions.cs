namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>Credential creation options produced by the ceremony service, carried to the browser and persisted for verification.</summary>
public class WebAuthnAttestationOptions {
    /// <summary>`PublicKeyCredentialCreationOptions` serialized as JSON.</summary>
    public required string OptionsJson { get; init; }
}
