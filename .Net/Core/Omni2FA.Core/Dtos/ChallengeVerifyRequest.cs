namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/challenge/verify</c> — final step of the login flow.</summary>
public class ChallengeVerifyRequest {
    /// <summary>Id of the method being verified — must match the one passed to <c>start</c>.</summary>
    public required Guid MethodId { get; init; }

    /// <summary>
    /// For TOTP and Email — the 6-digit numeric code.
    /// For WebAuthn — base64url-encoded assertion JSON.
    /// </summary>
    public required string Code { get; init; }
}
