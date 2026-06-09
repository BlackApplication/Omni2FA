namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/enroll/email/start</c>.</summary>
public class EmailEnrollStartRequest {
    /// <summary>
    /// Destination address the OTP is sent to. Honored only when
    /// <c>EmailEnrollmentAddressSource.HostSupplied</c> is configured — the host then owns address
    /// verification. Under the default <c>ClaimOnly</c> source this is ignored and the address is
    /// derived from the authenticated identity, so it is optional.
    /// </summary>
    public string? Email { get; init; }
}
