namespace Omni2FA.Core.Enums;

/// <summary>
/// Outcome of evaluating a step-up-protected request. Framework-agnostic — the ASP.NET layer maps it
/// to an HTTP response.
/// </summary>
public enum StepUpVerdict {
    /// <summary>A valid, unused step-up token was presented and has now been consumed. Allow the action.</summary>
    Satisfied,

    /// <summary>The user has no active 2FA method, so step-up does not apply. Allow the action (the design's only bypass).</summary>
    NotEnrolledBypass,

    /// <summary>The user has 2FA but presented no valid, unused token. Block and signal the frontend to confirm 2FA.</summary>
    Required,
}
