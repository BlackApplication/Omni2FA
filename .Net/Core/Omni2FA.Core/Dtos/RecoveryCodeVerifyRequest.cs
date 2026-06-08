namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/challenge/recovery-code</c>.</summary>
public class RecoveryCodeVerifyRequest {
    /// <summary>A single unused recovery code. Dashes/spaces and case are normalized server-side.</summary>
    public required string RecoveryCode { get; init; }
}
