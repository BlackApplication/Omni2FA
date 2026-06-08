namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/enroll/webauthn/confirm</c>.</summary>
public class WebAuthnEnrollConfirmRequest {
    /// <summary>Value returned from <c>/enroll/webauthn/start</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>The JSON produced by <c>navigator.credentials.create()</c>.</summary>
    public required string AttestationResponseJson { get; init; }

    /// <summary>Optional human-readable label for this credential.</summary>
    public string? Name { get; init; }
}
