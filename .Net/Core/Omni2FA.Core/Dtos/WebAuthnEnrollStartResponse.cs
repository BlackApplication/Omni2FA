namespace Omni2FA.Core.Dtos;

/// <summary>Returned by <c>POST /api/2fa/enroll/webauthn/start</c>.</summary>
public class WebAuthnEnrollStartResponse {
    /// <summary>Identifier of the pending ceremony. Pass back unchanged to <c>/confirm</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>`PublicKeyCredentialCreationOptions` JSON for <c>navigator.credentials.create()</c>.</summary>
    public required string OptionsJson { get; init; }
}
