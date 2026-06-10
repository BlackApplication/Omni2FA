namespace Omni2FA.Core.Dtos;

/// <summary>
/// Returned by <c>POST /api/2fa/stepup/verify</c> on success. The frontend attaches
/// <see cref="StepUpToken"/> in the step-up header when retrying the protected action. The token is
/// single-use — it satisfies exactly one protected call.
/// </summary>
public class StepUpVerifyResponse {
    /// <summary>Single-use proof that the step-up challenge passed, presented in the step-up request header.</summary>
    public required string StepUpToken { get; init; }

    /// <summary>When <see cref="StepUpToken"/> expires (UTC).</summary>
    public required DateTime ExpiresAt { get; init; }
}
