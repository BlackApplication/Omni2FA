namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>Assertion (login) options produced by the ceremony service, carried to the browser and persisted for verification.</summary>
public class WebAuthnAssertionOptions {
    /// <summary>`PublicKeyCredentialRequestOptions` serialized as JSON.</summary>
    public required string OptionsJson { get; init; }
}
