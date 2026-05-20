namespace Omni2FA.Core.Dtos;

/// <summary>
/// Body for <c>POST /api/2fa/enroll/totp/confirm</c>. User submits the first 6-digit code
/// from their authenticator to prove the secret was transferred correctly.
/// </summary>
public class TotpEnrollConfirmRequest {
    /// <summary>Value returned from <c>/enroll/totp/start</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>6-digit code from the authenticator app.</summary>
    public required string Code { get; init; }

    /// <summary>Optional human-readable label for this method.</summary>
    public string? Name { get; init; }
}
