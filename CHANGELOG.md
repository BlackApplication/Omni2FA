# Changelog

All notable changes to Omni2FA will be documented in this file. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

## [0.3.0] — 2026-06-08

### Added

#### .NET
- WebAuthn (passkeys & FIDO2 security keys) enrollment endpoints: `POST /enroll/webauthn/start` (issues creation options) and `POST /enroll/webauthn/confirm` (verifies the attestation). WebAuthn login via `/challenge/start` (issues assertion options) and `/challenge/verify` (validates the assertion, updates the signature counter).
- `Omni2FA.WebAuthn` project filled in: `Fido2WebAuthnCeremonyService` on Fido2NetLib 4.x — resident keys, sign-count tracking, globally-unique credential ids, per-user cap (`MAX_METHODS_REACHED`).
- `IWebAuthnCeremonyService` in core (FIDO2-free interface + value objects) so orchestration never depends on the crypto library; `WebAuthnEnrollmentService` orchestrates ceremony + stores.
- `WebAuthnOptions` (RP id/name, allowed origins, `MaxCredentialsPerUser`) bound under `Omni2Fa:WebAuthn`.
- `ChallengeVerifyRequest.assertionResponseJson` and `ChallengeStartResponse.optionsJson` added; `code` is now optional.

#### TypeScript core (`@omni2fa/core`)
- WebAuthn browser marshaling (`startRegistration`, `startAuthentication`) — base64url ↔ ArrayBuffer, `navigator.credentials.create/get`.
- `webauthnEnrollmentMachine` (start → auto browser ceremony → confirm) and challenge-machine WebAuthn branch (auto-assert on pick). Client methods `startWebAuthnEnrollment`, `confirmWebAuthnEnrollment`.

#### React adapter (`@omni2fa/react`)
- `useWebAuthnEnrollment` + `useWebAuthnEnrollmentSelector`.

#### Protocol
- OpenAPI bumped to `0.3.0` with the WebAuthn enrollment endpoints and assertion fields.

#### Example (`examples/full`)
- Passkey enrollment dialog and passkey login path. `Omni2Fa:WebAuthn` configured for `localhost` / `http://localhost:5173`.

### Changed
- TypeScript DTO aliases consolidated from one file each into a single `types/dtos.ts` barrel (pure generated-type aliases aren't "concepts" — see `docs/CODE_STYLE.md` rule 1).

## [0.2.0] — 2026-06-08

### Added

#### .NET
- Email OTP enrollment endpoints under `/api/2fa`: `POST /enroll/email/start`, `POST /enroll/email/confirm`, `POST /enroll/email/resend`. The host supplies the destination address in the request body — Omni2FA does not read it from a claim and does not own address verification.
- Email OTP login: `POST /challenge/start` issues and sends a code for Email methods; `POST /challenge/resend` re-sends it (cooldown-guarded); `POST /challenge/verify` validates it.
- `IEmailSender` (transport) with a default MailKit/SMTP implementation registered via `TryAdd` — hosts replace it with their own email infrastructure. `IEmailMessageBuilder` (copy) with a default English implementation, overridable for localization.
- Background email delivery by default (`EmailOptions.BackgroundDelivery`, on): OTP endpoints return without waiting on SMTP — messages queue onto an in-process channel drained by a hosted worker, send failures are logged, not surfaced. Set false for inline (awaited) send. Implemented via `IEmailDispatcher`.
- `IEmailOtpService` primitive (generate, hash, send, verify) and `IEmailEnrollmentService` orchestrator. Codes are SHA-256 hashed at rest; verification is constant-time.
- `EmailOptions` (digits, TTL, resend cooldown, sender identity, SMTP) bound under `Omni2Fa:Email`.
- `TwoFactorMethod.EmailAddress` and `TwoFactorChallenge.EmailAddress` columns; `ITwoFactorChallengeStore.GetActiveLoginChallengeAsync` for method-keyed login challenges.

#### TypeScript core (`@omni2fa/core`)
- `emailEnrollmentMachine` (start → awaitingCode → confirming, with resend) and challenge-machine Email support (resend + `expiresAt`/`resendAvailableAt` in context).
- Client methods `startEmailEnrollment`, `confirmEmailEnrollment`, `resendEmailEnrollment`, `resendChallenge`.

#### React adapter (`@omni2fa/react`)
- `useEmailEnrollment` + `useEmailEnrollmentSelector`. `useChallenge` gains a `resend` action.

#### Protocol
- OpenAPI bumped to `0.2.0` with the Email enrollment endpoints, `/challenge/resend`, and `expiresAt`/`resendAvailableAt` on `ChallengeStartResponse`.

#### Example (`examples/full`)
- Switched from EF InMemory to **SQLite** (state survives restarts — needed for realistic challenge/login testing).
- Email OTP enrollment dialog and Email login path. SMTP points at a local catcher (`localhost:1025`, e.g. Mailpit) by default.

## [0.1.0] — 2026-05-22

### Added

#### .NET
- ASP.NET Core Minimal API endpoints under `/api/2fa`: `GET /methods`, `DELETE /methods/{id}`, `POST /enroll/totp/start`, `POST /enroll/totp/confirm`, `POST /challenge/start`, `POST /challenge/verify`.
- `AddOmni2Fa(...)` DI extension and `MapOmni2Fa()` route extension in `Omni2FA.AspNetCore`.
- `IUserContextAccessor` with default impl reading configurable claim from `HttpContext.User`.
- Custom `IEndpointFilter` validating pre-auth Bearer tokens (no dependency on the host's authentication pipeline).
- `Result` → `IResult` mapping via `ToHttpResult()` extension with centralized error-code → HTTP-status switch.
- `JwtPreAuthTokenIssuer` (HMAC-SHA256, configurable issuer/audience/TTL).
- `TotpService` built on `Otp.NET`; `DataProtectionSecretProtector` for at-rest secret encryption.
- EF Core store adapter in `Omni2FA.AspNetCore.EntityFrameworkCore` with configurable table names, schemas, and column lengths.
- Stringified user identifier (`string UserId`) — supports any host id type without generic spread.

#### TypeScript core (`@omni2fa/core`)
- Typed HTTP client over `openapi-fetch`, auto-attaches `Authorization: Bearer <pre-auth>`.
- `IStorage` abstraction with `MemoryStorage` (default), `SessionStorageStorage`, `LocalStorageStorage`.
- Three xstate v5 machines: `totpEnrollmentMachine`, `challengeMachine`, `methodsMachine`.
- `createOmni2Fa({ baseUrl, storage })` — one-call factory assembling client + actors.
- DTO types auto-generated from the OpenAPI contract via `openapi-typescript`.
- `Omni2FaApiError` carrying stable error code + HTTP status + structured details.

#### React adapter (`@omni2fa/react`)
- `Omni2FaProvider` + `useOmni2Fa()` context.
- `useTotpEnrollment`, `useChallenge`, `useMethods` — headless hooks with named action proxies.
- `useTotpEnrollmentSelector`, `useChallengeSelector`, `useMethodsSelector` — escape hatches for granular subscriptions.
- `useMethods({ autoLoad })` — auto-fetch on mount with opt-out.

#### Protocol
- OpenAPI 3.1 contract published in `Core/protocol/omni2fa.openapi.yaml`.
- Stable error code catalogue in `Core/protocol/ERROR_CODES.md`.

#### Documentation
- `docs/ARCHITECTURE.md` — framework-agnostic core / thin-adapter boundary rule with code review checklist.
- `docs/ASPNETCORE.md` — design decisions for the ASP.NET Core adapter.
- `docs/CODE_STYLE.md`, `docs/FLOWS.md`, `docs/ROADMAP.md`.
