using Omni2FA.Core.Dtos;

namespace Example.Backend.Dtos.Auth;

/// <summary>
/// Returned by <c>/auth/login</c> when the user has enrolled 2FA. Frontend uses
/// <c>PreAuthToken</c> as Bearer on subsequent <c>/api/2fa/challenge/*</c> calls,
/// then host issues the session via <c>/auth/login</c> follow-up isn't needed —
/// the verify response carries <c>userId</c> which frontend posts to <c>/auth/finalize</c>.
/// </summary>
public class TwoFactorChallengeResponse {
    public required string PreAuthToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required IReadOnlyList<TwoFactorMethodDto> AvailableMethods { get; init; }
}
