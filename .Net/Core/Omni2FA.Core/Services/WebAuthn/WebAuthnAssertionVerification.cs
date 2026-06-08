namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>Result of a verified assertion — the updated signature counter to persist back on the method.</summary>
public class WebAuthnAssertionVerification {
    /// <summary>Updated signature counter reported by the authenticator.</summary>
    public required uint SignCount { get; init; }
}
