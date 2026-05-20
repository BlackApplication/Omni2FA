using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Dtos;

/// <summary>
/// Response from <c>POST /api/2fa/challenge/start</c>. For TOTP — empty payload, just an
/// acknowledgement. For Email — confirms an OTP was sent. For WebAuthn — carries assertion
/// request options.
/// </summary>
public class ChallengeStartResponse {
    /// <summary>Kind of the method that was started.</summary>
    public required TwoFactorMethodKind Kind { get; init; }
}
