namespace Omni2FA.Core.Dtos;

/// <summary>
/// Returned by <c>POST /api/2fa/challenge/verify</c> on success. The host uses
/// <see cref="UserId"/> to mint its own session JWT/cookie.
/// </summary>
public class VerifySuccessResponse {
    /// <summary>Always <c>true</c> on this response. Failures use <c>4xx</c> with an error envelope.</summary>
    public required bool Verified { get; init; }

    /// <summary>The verified user's id.</summary>
    public required Guid UserId { get; init; }
}
