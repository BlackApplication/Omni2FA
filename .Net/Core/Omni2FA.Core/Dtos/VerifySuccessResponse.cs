namespace Omni2FA.Core.Dtos;

/// <summary>
/// Returned by <c>/challenge/verify</c> and <c>/challenge/recovery-code</c> on success. The frontend
/// forwards <see cref="VerifiedToken"/> to the host's finalize endpoint to mint the session.
/// </summary>
public class VerifySuccessResponse {
    /// <summary>Verified user's id. Informational only — the host derives the user from <see cref="VerifiedToken"/>, not this.</summary>
    public required string UserId { get; init; }

    /// <summary>Proof the ceremony passed. The host validates it with <c>IPreAuthTokenIssuer.ValidateVerified</c> in finalize.</summary>
    public required string VerifiedToken { get; init; }

    /// <summary>When <see cref="VerifiedToken"/> expires (UTC).</summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Step-up token granted by the login itself, so a user who just proved 2FA is not asked again on the
    /// first protected action. Null unless the host set <see cref="Configuration.StepUpOptions.GraceWindow"/>,
    /// and never issued for a recovery-code login. Unrelated to <see cref="VerifiedToken"/> — that one
    /// finalizes the session, this one satisfies the step-up barrier until <see cref="StepUpGraceUntil"/>.
    /// </summary>
    public string? StepUpToken { get; init; }

    /// <summary>Until when <see cref="StepUpToken"/> keeps satisfying protected calls (UTC). Null when there is no token.</summary>
    public DateTime? StepUpGraceUntil { get; init; }
}
