import type { components } from './api';

/**
 * Named aliases for the wire DTOs, projected from the OpenAPI-generated `components['schemas']`.
 * These are pure type aliases (no runtime presence), so they live together here rather than one
 * file each — a file per alias is noise. Real values (const groups, enums) still get their own file.
 */
type Schemas = components['schemas'];

export type ChallengeResendRequest = Schemas['ChallengeResendRequest'];
export type ChallengeStartRequest = Schemas['ChallengeStartRequest'];
export type ChallengeStartResponse = Schemas['ChallengeStartResponse'];
export type ChallengeVerifyRequest = Schemas['ChallengeVerifyRequest'];
export type EmailEnrollStartRequest = Schemas['EmailEnrollStartRequest'];
export type EmailEnrollStartResponse = Schemas['EmailEnrollStartResponse'];
export type EmailEnrollConfirmRequest = Schemas['EmailEnrollConfirmRequest'];
export type EmailEnrollResendRequest = Schemas['EmailEnrollResendRequest'];
export type ErrorResponse = Schemas['ErrorResponse'];
export type MethodCreatedResponse = Schemas['MethodCreatedResponse'];
export type PreAuthChallengeResponse = Schemas['PreAuthChallengeResponse'];
export type TotpEnrollConfirmRequest = Schemas['TotpEnrollConfirmRequest'];
export type TotpEnrollStartResponse = Schemas['TotpEnrollStartResponse'];
export type WebAuthnEnrollStartResponse = Schemas['WebAuthnEnrollStartResponse'];
export type WebAuthnEnrollConfirmRequest = Schemas['WebAuthnEnrollConfirmRequest'];
export type RecoveryCodesResponse = Schemas['RecoveryCodesResponse'];
export type RecoveryCodeVerifyRequest = Schemas['RecoveryCodeVerifyRequest'];
export type TwoFactorMethodDto = Schemas['TwoFactorMethodDto'];
export type TwoFactorMethodType = Schemas['TwoFactorMethodType'];
export type VerifySuccessResponse = Schemas['VerifySuccessResponse'];
export type StepUpVerifyResponse = Schemas['StepUpVerifyResponse'];
