namespace Omni2FA.Core.Errors;

/// <summary>
/// Stable catalogue of error codes returned in <see cref="Dtos.ErrorResponse.Code"/>.
/// One source of truth for all .NET adapters — never hard-code these strings inline.
/// </summary>
public static class Omni2FaErrorCodes {
    /// <summary>Submitted OTP code didn't validate (TOTP or Email).</summary>
    public const string InvalidCode = "INVALID_CODE";

    /// <summary>Pre-auth token has expired.</summary>
    public const string PreAuthExpired = "PREAUTH_EXPIRED";

    /// <summary>Pre-auth token missing, malformed, or signed by an unknown key.</summary>
    public const string PreAuthInvalid = "PREAUTH_INVALID";

    /// <summary>No active challenge for the given method under the current pre-auth token.</summary>
    public const string ChallengeNotFound = "CHALLENGE_NOT_FOUND";

    /// <summary>Challenge was already consumed by a previous successful verify or revoked.</summary>
    public const string ChallengeConsumed = "CHALLENGE_CONSUMED";

    /// <summary>Rate limit hit. Frontend honors <c>Retry-After</c>.</summary>
    public const string TooManyAttempts = "TOO_MANY_ATTEMPTS";

    /// <summary>The method id does not exist, or doesn't belong to the current user.</summary>
    public const string MethodNotFound = "METHOD_NOT_FOUND";

    /// <summary>Attempt to enroll a unique-per-user kind (TOTP or Email) the user already has.</summary>
    public const string KindAlreadyEnrolled = "KIND_ALREADY_ENROLLED";

    /// <summary>Attempt to enroll a WebAuthn credential beyond the configured per-user cap.</summary>
    public const string MaxMethodsReached = "MAX_METHODS_REACHED";

    /// <summary>Host policy forbids removing the user's last active method.</summary>
    public const string LastMethodProtected = "LAST_METHOD_PROTECTED";

    /// <summary>Submitted recovery code doesn't match any stored hash.</summary>
    public const string RecoveryCodeInvalid = "RECOVERY_CODE_INVALID";

    /// <summary>Recovery code matched a hash but the code was already used.</summary>
    public const string RecoveryCodeUsed = "RECOVERY_CODE_USED";

    /// <summary>WebAuthn assertion failed validation against the stored credential.</summary>
    public const string WebAuthnVerificationFailed = "WEBAUTHN_VERIFICATION_FAILED";

    /// <summary>Request body failed structural validation.</summary>
    public const string ValidationFailed = "VALIDATION_FAILED";
}
