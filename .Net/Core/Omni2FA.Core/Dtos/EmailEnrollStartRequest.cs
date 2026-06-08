namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/enroll/email/start</c>.</summary>
public class EmailEnrollStartRequest {
    /// <summary>
    /// Destination address the OTP is sent to, supplied by the host. Omni2FA does not derive it
    /// from a claim and does not verify address ownership — that is the host's responsibility.
    /// </summary>
    public required string Email { get; init; }
}
