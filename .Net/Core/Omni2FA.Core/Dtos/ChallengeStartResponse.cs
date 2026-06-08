using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Dtos;

/// <summary>
/// Response from <c>POST /api/2fa/challenge/start</c>. For TOTP — empty payload, just an
/// acknowledgement. For Email — confirms an OTP was sent. For WebAuthn — carries assertion
/// request options.
/// </summary>
public class ChallengeStartResponse {
    /// <summary>Type of the method that was started.</summary>
    public required TwoFactorMethodType Type { get; init; }

    /// <summary>UTC. For Email — when the sent code stops validating. Null for TOTP.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>UTC. For Email — earliest time a resend is permitted. Null for TOTP.</summary>
    public DateTime? ResendAvailableAt { get; init; }

    /// <summary>For WebAuthn — `PublicKeyCredentialRequestOptions` JSON for <c>navigator.credentials.get()</c>. Null otherwise.</summary>
    public string? OptionsJson { get; init; }
}
