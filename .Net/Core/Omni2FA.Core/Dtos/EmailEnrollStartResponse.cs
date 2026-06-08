namespace Omni2FA.Core.Dtos;

/// <summary>Returned by <c>POST /api/2fa/enroll/email/start</c> and <c>/enroll/email/resend</c>.</summary>
public class EmailEnrollStartResponse {
    /// <summary>Identifier of the pending enrollment ceremony. Pass back unchanged to <c>/confirm</c> and <c>/resend</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>UTC. When the emailed code stops validating.</summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>UTC. Earliest time a resend is permitted (cooldown).</summary>
    public required DateTime ResendAvailableAt { get; init; }
}
