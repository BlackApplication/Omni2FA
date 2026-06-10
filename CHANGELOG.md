# Changelog

All notable changes to Omni2FA will be documented in this file. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

## [0.8.0] — 2026-06-10

Extends step-up to the library's **own** sensitive endpoints. The endpoints `MapOmni2Fa` mounts (remove
method, regenerate recovery codes, enroll a new factor) can't be decorated by the host, so they're gated
by opt-in config flags, and the JS client confirms + retries them automatically. Minor bump: those
endpoints can now return `403 STEP_UP_REQUIRED`, and the client API + config grow.

### Added
- **Per-action step-up flags (.NET)** — `StepUp.RequireTwoFactorToEnroll` / `...ToRemoveMethod` / `...ToRegenerateRecoveryCodes` (all default `false`). When on, `MapOmni2Fa` applies `.RequireStepUp()` to the matching endpoint (`/enroll/*/start`, `DELETE /methods/{id}`, `/recovery-codes/regenerate`). A user with no active method is never blocked (`NotEnrolledBypass`).
- **Client-side step-up handling (`@omni2fa/core`)** — `IOmni2FaClient.setStepUpHandler(handler)`: when one of the client's own sensitive calls returns `403 STEP_UP_REQUIRED`, the client invokes the handler (e.g. the React `confirmTwoFactor`) and retries with the `X-Omni2FA-StepUp` header. Covers `removeMethod`, `regenerateRecoveryCodes`, and the three enroll-`start` calls — whether called directly or via hooks.

### Changed
- OpenAPI `0.8.0`: the five gated operations document a `403 STEP_UP_REQUIRED` response (returned only when the host enabled the matching flag).
- Docs (`README`, `ARCHITECTURE`, `ASPNETCORE`, `FLOWS`) and the `examples/full` host (flags on; a shared `StepUpDialog` + a `StepUpModalHost` that registers the handler) updated.

### Security
- Closes a gap from 0.7.3: the library's own destructive endpoints (notably remove-method and enroll-a-new-factor — a session-theft persistence vector) had no step-up path and couldn't be protected from outside. Recovery codes remain one-way hashed: there is no "view existing codes", so the gated recovery action is *regenerate*, not view.

## [0.7.3] — 2026-06-10

Adds **step-up authentication** — a strict, single-use 2FA confirmation gate for sensitive actions
(change password, view recovery codes, remove a method), independent of the login flow. OpenAPI moves
to `0.7.3`; the .NET and all TypeScript packages bump to match.

### Added
- **Step-up barrier (.NET)** — `[RequireTwoFactor]` (MVC action filter) and `.RequireStepUp()` (minimal-API endpoint filter) gate any endpoint: an enrolled user must present a valid step-up token or the call returns `403 STEP_UP_REQUIRED` (carrying the available methods); a user with no 2FA passes through. The decision lives once in `Omni2FA.Core`'s `IStepUpEvaluator`. New `/api/2fa/stepup/start|resend|verify` endpoints (session-authenticated mirrors of `/challenge/*`); `verify` mints a single-use step-up token (`purpose=2fa-stepup`) via the new `IPreAuthTokenIssuer.IssueStepUp` / `ValidateStepUp`. New `StepUp.Ttl` + header-name options.
- **Single-use enforcement** — `IStepUpNonceStore` records spent token ids until expiry; default `InMemoryStepUpNonceStore` is single-instance (register a shared store, e.g. Redis, for multi-node — otherwise a token spent on one node has a replay window on the others bounded by the TTL). The token is bound to the caller, so a stolen token can't be replayed against another account.
- **Step-up (`@omni2fa/core`)** — `client.startStepUp` / `resendStepUp` / `verifyStepUp`, the `stepUpMachine`, the `STEP_UP_HEADER` constant, and the `STEP_UP_REQUIRED` error code. Transport-agnostic by design — the library never makes the protected request, so cookie- and Bearer-session hosts integrate identically.
- **Step-up (`@omni2fa/react`)** — `useStepUp()` returning `confirmTwoFactor(methods)` (shows the prompt, resolves a single-use token) plus the prompt state (`active`, `methods`, `status`, `pick`/`submit`/`resend`/`cancel`); reuses the existing challenge UI. The host detects `403 STEP_UP_REQUIRED` and replays the request with the header in its own fetch/axios layer.

### Changed
- `ITwoFactorChallengeService` gains `VerifyStepUpAsync`; login and step-up share one verification core (no behavior change to login).
- Docs (`README`, `ARCHITECTURE`, `FLOWS`, `ASPNETCORE`, `ERROR_CODES`) and the `examples/full` host (a step-up-protected `POST /user/change-password`) updated.

## [0.7.2] — 2026-06-09

Patch: EF Core 10 host compatibility. .NET packages only — no API contract change, so OpenAPI stays
at `0.7.1` and the TypeScript packages are unchanged.

### Fixed
- **EF Core 10 host compatibility (`MissingMethodException` on bulk delete)** — `Omni2FA.AspNetCore.EntityFrameworkCore` now multi-targets `net8.0;net10.0`, compiling each build against its matching EF Core major (8.0.x / 10.0.x). The previous single `net8.0` build bound `ExecuteDeleteAsync` to EF Core 8's `RelationalQueryableExtensions`; under a host running EF Core 10 that method has moved, so recovery-code wipe and challenge purge threw `MissingMethodException` at runtime. NuGet now hands each host the matching asset. `Omni2FA.Core` and `Omni2FA.AspNetCore` stay `net8.0` (consumed down-level by net10 hosts).

## [0.7.1] — 2026-06-09

Closes an email-enrollment foot-gun surfaced in live integration: the OTP destination was taken
verbatim from the request body, so every host had to remember to substitute an authoritative,
verified address — and any host that forgot enrolled whatever the caller sent. Omni2FA now derives
the address from the authenticated identity by default. OpenAPI moves to `0.7.1`.

### Added
- **`IUserContextAccessor.GetCurrentUserEmail()`** — resolves the current user's email from the configured `AspNetCoreOptions.UserEmailClaim` (default `ClaimTypes.Email`, falling back to the raw JWT `email` claim). Kept distinct from `UserLabelClaim` because the OTP destination is security-sensitive, not cosmetic. `UserContextAccessor` methods are now `virtual`, so a host with a non-claim source overrides this one method instead of writing endpoint glue.
- **`AspNetCoreOptions.EmailEnrollmentAddressSource`** — `ClaimOnly` (default) derives the address from the identity; `HostSupplied` preserves the previous body-supplied behavior for hosts that legitimately enroll an address other than the signed-in one.

### Changed
- **`POST /enroll/email/start` is secure by default** — under `ClaimOnly` the body `email` is ignored and the address comes from `GetCurrentUserEmail()`. `EmailEnrollStartRequest.Email` is now optional (was required); the TS client/machine and the `useEmailEnrollment` hook's `start(email?)` accept an omitted address accordingly.

### Migration
- Hosts relying on the request-body address (e.g. a decorator that injected the user's email) can delete that glue — the default now does it. Hosts that intentionally enroll a *different* address than the identity claim must set `EmailEnrollmentAddressSource = HostSupplied`.

### Refactor
- Extracted repeated store idioms into `ChallengeStoreExtensions` (`GetActiveEnrollmentAsync`, `RecordFailedAttemptAsync`, `AddAndSaveAsync`) and reused them across the Email/TOTP/WebAuthn enrollment services and the challenge service. Centralizes the "matching challenge kind" guard and the write-then-save pairs; no behavior change. `UserContextAccessor`'s three claim lookups now share a `FindClaimValue` helper.

## [0.7.0] — 2026-06-08

Compatibility release from live-integration feedback: gives hosts a clean "2FA actually passed" signal
instead of forcing them to reconstruct one from audit events. OpenAPI moves to `0.7.0`.

### Added
- **Verified-handoff token** — `challenge/verify` and `challenge/recovery-code` now return a short-lived `verifiedToken` (`purpose=2fa-verified`) alongside `userId`. The frontend forwards it to the host's finalize endpoint, which calls the new `IPreAuthTokenIssuer.ValidateVerified(token)` to recover the trusted user id and mint the session. New `IssueVerified` / `ValidateVerified` on `IPreAuthTokenIssuer`; `PreAuth.VerifiedTtl` option (default 2 min). `VerifySuccessResponse` gains `verifiedToken` + `expiresAt`; surfaced in the TS challenge machine as `context.verifiedToken`.

### Fixed
- **2FA bypass in the host finalize pattern** — finalize must validate the *verified-handoff* token, not the pre-auth token. The pre-auth token is minted right after the password step, so re-validating it (as the example previously did) let anyone who passed the password — but not 2FA — mint a session. The example now validates `verifiedToken`.
- **Recovery-code "verified" signal** — a recovery-code login previously emitted only the `RecoveryCodeUsed` audit event, so a host inferring success from `LoginVerifySucceeded` silently rejected it. Both paths now return the same `verifiedToken`, so recovery is no longer a special case. This also removes the per-user race in audit-based gates: the token is bound to the ceremony, not the user.

### Changed
- Docs (`README`, `FLOWS`, `ASPNETCORE`) and the `examples/full` host updated to the verified-handoff finalize flow.

## [0.6.1] — 2026-06-08

Patch: fixes and a refactor from a live end-to-end run + multi-agent code review. No contract change —
OpenAPI stays at `0.6.0`.

### Fixed
- **Enum wire format** — `TwoFactorMethodType` now serializes as a string (`"Totp"`) via an attribute on the enum, matching the OpenAPI contract and the TS types regardless of host JSON settings. Previously emitted as an integer, silently breaking every `type === 'Totp'` check on the frontend.
- **Rate limiting** — the shared window now lives in a singleton (`Omni2FaRateLimiter`) resolved per request, so all sensitive endpoints actually share one IP partition. Previously the limiter never engaged.
- **Example 2FA bypass** — the host example's `POST /auth/finalize` now derives the user from the validated pre-auth token instead of trusting a `userId` in the request body (which let anyone mint a session for any user).
- Validate `Omni2Fa:PreAuth:SigningKey` length at startup (`ValidateOnStart`); background `ChallengePurgeBackgroundService` prunes expired challenges; unique index + uncapped length on WebAuthn credential columns; token routing classifies endpoints by path under the mount, not a URL substring; example enables forwarded headers for correct client IP behind a proxy.

### Refactor
- Extracted the shared enrollment tail (first-method recovery codes + `MethodEnrolled` audit) into `IEnrollmentFinalizer`, removing the duplication across the three enrollment services.
- **Packaging:** folded the standalone `Omni2FA.WebAuthn` package into `Omni2FA.AspNetCore` — the .NET side now ships **3** NuGet packages instead of 4. The `IWebAuthnCeremonyService` contract stays in `Omni2FA.Core` (core remains FIDO2-free); only the Fido2NetLib implementation moved into the adapter. Consumers still just install `Omni2FA.AspNetCore` (+ the EF store).

## [0.6.0] — 2026-06-08

Production-hardening release: recovery codes (v0.4 scope) plus the v0.6 stabilization items, so
Omni2FA can be deployed to a real app for testing. The v0.5 `@omni2fa/react-mui` styled package is
intentionally deferred — hosts use the headless `@omni2fa/react` hooks (see the example).

### Added

#### Recovery codes (.NET)
- One-time backup codes: generated on first method enrollment (returned once in `MethodCreatedResponse.recoveryCodes`), `POST /recovery-codes/regenerate`, and `POST /challenge/recovery-code` as a method-agnostic login fallback. `XXXX-XXXX-XX` format, SHA-256 hashed at rest, one-time use. New `RecoveryCode` entity + `IRecoveryCodeStore` (EF adapter, configurable `RecoveryCodesTableName`) + `IRecoveryCodeService`. Wiped when a user's last method is removed.

#### Hardening (.NET)
- **Rate limiting** — IP-partitioned fixed-window limiter (`RateLimitFilter`, default 20/min/IP) on the sensitive endpoints (challenge verify/resend/recovery-code, enroll groups). Self-contained — no host `UseRateLimiter`. Returns `429 TOO_MANY_ATTEMPTS` + `Retry-After`. Configurable via `Omni2Fa:RateLimit`.
- **Audit** — `IOmni2FaAuditSink` raising MethodEnrolled/Removed, LoginVerifySucceeded/Failed, RecoveryCodes{Generated,Regenerated}, RecoveryCodeUsed, RateLimitExceeded. Default `LoggerAuditSink` (structured `ILogger`) registered via `TryAdd`; hosts replace it.
- **Last-method policy** — `AspNetCore.AllowDisablingLastMethod` (default true); when false, removing the last method returns `409 LAST_METHOD_PROTECTED`.

#### Client (`@omni2fa/core`)
- **Session-token API** — `setSessionToken`/`getSessionToken`, `credentials` config, `sessionStorageKey`. The request middleware now routes the pre-auth token to `/challenge/*` and the host session token to everything else, removing the custom-fetch workaround. `regenerateRecoveryCodes` / `verifyRecoveryCode` client methods; challenge-machine recovery-code branch; enrollment machines surface `recoveryCodes`.

#### React (`@omni2fa/react`)
- `useChallenge` gains `useRecoveryCode`.

#### Protocol
- OpenAPI `0.6.0`: recovery-code endpoints + `MethodCreatedResponse.recoveryCodes`.

#### Example (`examples/full`)
- Recovery codes shown once after first enrollment, regenerate button, recovery-code login path. `omni2fa.ts` simplified to use the session-token API (custom fetch removed).

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
