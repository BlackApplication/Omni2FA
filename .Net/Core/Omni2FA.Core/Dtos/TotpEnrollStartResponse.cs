namespace Omni2FA.Core.Dtos;

/// <summary>
/// Returned by <c>POST /api/2fa/enroll/totp/start</c>. Contains everything the frontend
/// needs to render a QR code (or manual-entry secret) for the user's authenticator app.
/// </summary>
public class TotpEnrollStartResponse {
    /// <summary>Identifier of the pending enrollment ceremony. Pass back unchanged to <c>/confirm</c>.</summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>
    /// <c>otpauth://</c> URI for QR rendering. Format:
    /// <c>otpauth://totp/{Issuer}:{accountLabel}?secret={base32}&amp;issuer={Issuer}&amp;algorithm=SHA1&amp;digits=6&amp;period=30</c>.
    /// </summary>
    public required string OtpAuthUri { get; init; }

    /// <summary>Base32-encoded TOTP secret. Shown as a fallback for manual entry when QR scanning fails.</summary>
    public required string Secret { get; init; }
}
