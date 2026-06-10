namespace Omni2FA.Core.Configuration;

/// <summary>
/// Step-up (action-confirmation) settings. A step-up-protected endpoint requires the user to
/// re-confirm 2FA right before a sensitive action (change password, view recovery codes, remove a
/// method). The proof is a short-lived, single-use JWT issued by the step-up challenge and presented
/// in <see cref="HeaderName"/>. Signing reuses <see cref="PreAuthOptions.SigningKey"/>; only the
/// lifetime and header are configured here. There is intentionally no switch to weaken the barrier —
/// the only built-in bypass is "user has no 2FA enrolled", which is part of the design.
/// </summary>
public class StepUpOptions {
    /// <summary>
    /// How long an issued step-up token stays valid before the user must re-confirm. Default 5 minutes.
    /// The token is single-use regardless, so this only bounds the gap between confirming 2FA and
    /// performing the action.
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Request header carrying the step-up token on a protected call. Default <c>X-Omni2FA-StepUp</c>.</summary>
    public string HeaderName { get; set; } = "X-Omni2FA-StepUp";
}
