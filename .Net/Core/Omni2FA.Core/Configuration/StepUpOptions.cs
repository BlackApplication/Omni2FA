namespace Omni2FA.Core.Configuration;

/// <summary>
/// Step-up (action-confirmation) settings. A step-up-protected endpoint requires the user to
/// re-confirm 2FA right before a sensitive action (change password, view recovery codes, remove a
/// method). The proof is a short-lived, single-use JWT issued by the step-up challenge and presented
/// in <see cref="HeaderName"/>. Signing reuses <see cref="PreAuthOptions.SigningKey"/>; only the
/// lifetime, header, and grace window are configured here. There is intentionally no switch to weaken
/// the barrier for an enrolled user beyond <see cref="GraceWindow"/> — the only built-in bypass is
/// "user has no 2FA enrolled", which is part of the design.
/// </summary>
public class StepUpOptions {
    /// <summary>
    /// How long an issued step-up token stays valid before the user must re-confirm. Default 5 minutes.
    /// Outside <see cref="GraceWindow"/> the token is single-use, so this only bounds the gap between
    /// confirming 2FA and performing the action.
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a passed 2FA challenge keeps satisfying protected calls before the user is asked again.
    /// Counts either ceremony — a step-up confirmation or the 2FA login itself — because both prove the
    /// same thing. Default <see cref="TimeSpan.Zero"/> — every action confirms separately; 30–120 seconds
    /// is the useful range. Must not exceed <see cref="Ttl"/> (validated at startup).
    /// <para>
    /// The window travels inside the token, so only the browser that passed the challenge reuses it —
    /// another session of the same user still confirms. Signing in with a <em>recovery code</em> never
    /// counts: that is the flow an attacker who took the account over uses, and it must not open the door
    /// to removing the owner's methods.
    /// </para>
    /// </summary>
    public TimeSpan GraceWindow { get; set; } = TimeSpan.Zero;

    /// <summary>Request header carrying the step-up token on a protected call. Default <c>X-Omni2FA-StepUp</c>.</summary>
    public string HeaderName { get; set; } = "X-Omni2FA-StepUp";

    /// <summary>
    /// Require a step-up confirmation to enroll a new 2FA method (gates the <c>/enroll/*/start</c>
    /// endpoints). Stops a stolen session from silently adding an attacker-controlled factor. A user with
    /// no method yet is never blocked (nothing to confirm against). Default <c>false</c> — opt in.
    /// </summary>
    public bool RequireTwoFactorToEnroll { get; set; }

    /// <summary>
    /// Require a step-up confirmation to remove a 2FA method (gates <c>DELETE /methods/{id}</c>).
    /// Default <c>false</c> — opt in.
    /// </summary>
    public bool RequireTwoFactorToRemoveMethod { get; set; }

    /// <summary>
    /// Require a step-up confirmation to regenerate recovery codes (gates <c>/recovery-codes/regenerate</c>),
    /// which invalidates the existing set. Default <c>false</c> — opt in.
    /// </summary>
    public bool RequireTwoFactorToRegenerateRecoveryCodes { get; set; }
}
