namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/enroll/email/resend</c>.</summary>
public class EmailEnrollResendRequest {
    /// <summary>Value returned from <c>/enroll/email/start</c>.</summary>
    public required Guid EnrollmentId { get; init; }
}
