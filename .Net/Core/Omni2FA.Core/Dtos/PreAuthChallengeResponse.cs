namespace Omni2FA.Core.Dtos;

/// <summary>
/// Returned by the host's login endpoint when the verified user has 2FA methods enrolled.
/// Carries the short-lived pre-auth token and the list of methods the user can pick from.
/// </summary>
public class PreAuthChallengeResponse {
    /// <summary>
    /// Short-lived JWT. Sent as a Bearer token to every <c>/api/2fa/challenge/*</c> endpoint
    /// until verification completes.
    /// </summary>
    public required string PreAuthToken { get; init; }

    /// <summary>Active methods the user can pick to verify with.</summary>
    public required IReadOnlyList<TwoFactorMethodDto> AvailableMethods { get; init; }

    /// <summary>When the <see cref="PreAuthToken"/> expires (UTC).</summary>
    public required DateTime ExpiresAt { get; init; }
}
