import { Omni2FaErrorCodes, type Omni2FaErrorCode } from './codes';

const defaults: Record<Omni2FaErrorCode, string> = {
    [Omni2FaErrorCodes.InvalidCode]: 'The code you entered is invalid.',
    [Omni2FaErrorCodes.PreAuthExpired]: 'Your session has expired. Please sign in again.',
    [Omni2FaErrorCodes.PreAuthInvalid]: 'Your session is invalid. Please sign in again.',
    [Omni2FaErrorCodes.ChallengeNotFound]: 'No active verification step. Please restart.',
    [Omni2FaErrorCodes.ChallengeConsumed]: 'This verification step was already used. Please sign in again.',
    [Omni2FaErrorCodes.TooManyAttempts]: 'Too many attempts. Please wait before trying again.',
    [Omni2FaErrorCodes.MethodNotFound]: 'The selected 2FA method was not found.',
    [Omni2FaErrorCodes.TypeAlreadyEnrolled]: 'You already have this type of 2FA enabled.',
    [Omni2FaErrorCodes.MaxMethodsReached]: 'You have reached the maximum number of 2FA methods.',
    [Omni2FaErrorCodes.LastMethodProtected]: 'You cannot remove your last 2FA method.',
    [Omni2FaErrorCodes.RecoveryCodeInvalid]: 'The recovery code is invalid.',
    [Omni2FaErrorCodes.RecoveryCodeUsed]: 'This recovery code has already been used.',
    [Omni2FaErrorCodes.WebAuthnVerificationFailed]: 'Security key verification failed.',
    [Omni2FaErrorCodes.ValidationFailed]: 'The request was malformed.',
    [Omni2FaErrorCodes.StepUpRequired]: 'Please confirm two-factor authentication to continue.',
    [Omni2FaErrorCodes.NetworkError]: 'Network error. Please check your connection.',
    [Omni2FaErrorCodes.Unknown]: 'An unexpected error occurred.',
};

export function getDefaultMessage(code: string): string {
    return defaults[code as Omni2FaErrorCode] ?? defaults[Omni2FaErrorCodes.Unknown];
}
