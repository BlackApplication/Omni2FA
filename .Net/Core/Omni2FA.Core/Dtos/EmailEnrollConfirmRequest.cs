namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/enroll/email/confirm</c>.</summary>
public class EmailEnrollConfirmRequest {
    /// <summary>Value returned from <c>/enroll/email/start</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>The numeric code from the enrollment email.</summary>
    public required string Code { get; init; }

    /// <summary>Optional human-readable label for this method.</summary>
    public string? Name { get; init; }
}
