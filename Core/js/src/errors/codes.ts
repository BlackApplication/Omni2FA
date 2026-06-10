export const Omni2FaErrorCodes = {
    InvalidCode: 'INVALID_CODE',
    PreAuthExpired: 'PREAUTH_EXPIRED',
    PreAuthInvalid: 'PREAUTH_INVALID',
    ChallengeNotFound: 'CHALLENGE_NOT_FOUND',
    ChallengeConsumed: 'CHALLENGE_CONSUMED',
    TooManyAttempts: 'TOO_MANY_ATTEMPTS',
    MethodNotFound: 'METHOD_NOT_FOUND',
    TypeAlreadyEnrolled: 'TYPE_ALREADY_ENROLLED',
    MaxMethodsReached: 'MAX_METHODS_REACHED',
    LastMethodProtected: 'LAST_METHOD_PROTECTED',
    RecoveryCodeInvalid: 'RECOVERY_CODE_INVALID',
    RecoveryCodeUsed: 'RECOVERY_CODE_USED',
    WebAuthnVerificationFailed: 'WEBAUTHN_VERIFICATION_FAILED',
    ValidationFailed: 'VALIDATION_FAILED',
    StepUpRequired: 'STEP_UP_REQUIRED',
    NetworkError: 'NETWORK_ERROR',
    Unknown: 'UNKNOWN',
} as const;

export type Omni2FaErrorCode = (typeof Omni2FaErrorCodes)[keyof typeof Omni2FaErrorCodes];
