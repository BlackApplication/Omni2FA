# Changelog

All notable changes to Omni2FA will be documented in this file. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

## [0.10.1] — 2026-08-04

Stops step-up from asking the same person the same question over and over. Two complaints, one
mechanism: managing your 2FA meant confirming three times in a row, and signing in with 2FA then
opening a protected page meant confirming twice within seconds. A passed 2FA challenge — a step-up
ceremony *or* the login itself — now counts for a configurable window. **The window defaults to
off**, so a host that changes nothing sees the previous behavior exactly. Purely additive: the two
success responses gain optional fields, so OpenAPI is a patch to `0.8.1`.

### Added
- **`StepUp.GraceWindow` (`Omni2FA.Core`)** — how long a passed 2FA challenge keeps satisfying
  protected calls before prompting again (default `TimeSpan.Zero` = every action confirms separately;
  30–120s is the useful range). `StepUpEvaluator` returns `Satisfied` without spending the nonce inside
  the window, so the token keeps its single use for after it. One setting covers both ceremonies —
  a step-up confirmation and a 2FA login prove the same thing, so they are timed the same.
- **A 2FA login now clears the barrier for the same window** — `/challenge/verify` mints a step-up
  token alongside the handoff token and returns it as `stepUpToken` + `stepUpGraceUntil`, so opening a
  protected page right after signing in no longer asks a second time within seconds.
- **`StepUpVerifyResponse.graceUntil`** — until when the token keeps satisfying protected calls. The
  server is the only source of the window, so frontends need no matching setting.
- **`peekStepUpToken()` / `clearStepUpToken()` (`@omni2fa/core`)** — the live confirmation, for
  adapters and for hosts driving step-up by hand. The client caches it **in memory only** (never
  `storage`), fills it from both `verifyStepUp` and `verifyChallenge`, and drops it on sign-out
  (`setSessionToken(null)`) and on any `403 STEP_UP_REQUIRED`.
- **`useStepUp` (`@omni2fa/react`)** — `confirmTwoFactor` resolves from a live confirmation instead of
  showing the prompt. No API change; nothing to do in host code.

### Security
- **A recovery-code login never grants the window.** That is the flow an attacker who has taken the
  account over uses, and the first thing they do is remove the owner's methods — so
  `/challenge/recovery-code` returns no step-up token regardless of `GraceWindow`.
- The window is carried **inside the token** (an `omni2fa_grace_until` claim), not as a server-side
  "this user confirmed recently" record. A per-user record would extend the free pass to every session
  of that user — including a stolen one, which is the exact threat step-up answers. In the token it can
  only be reused by the browser that confirmed, and the evaluator still rejects a token whose subject
  isn't the caller. The residual cost is that script execution in that page gets a reusable
  confirmation for the window's duration rather than a single-use one.
- The window is validated at startup to be ≤ `StepUp.Ttl`. Note that `Ttl` itself is uncapped, so a
  host that sets both to hours turns the barrier into a formality — keep the window in minutes.
- `setSessionToken` clears the cached confirmation only when signing *out*. Clearing it on every call
  would discard the token the login just granted; swapping users without signing out is safe anyway,
  since the server binds every token to its subject.

### Changed
- `examples/full` turns the window on (`"GraceWindow": "00:01:00"`) — all three of its 2FA actions are
  gated, so it's where the friction shows.
- Docs (`README`, `ARCHITECTURE`, `ASPNETCORE`, `FLOWS`) updated; `FLOWS` §5 corrected to say trusted
  devices are v1.3, matching `ROADMAP` (it said v1.1).

### Migration
- Nothing to do — the window defaults to off and every new response field is optional.
- The in-memory cache dies with the tab. A host that does a **full page reload** after login (rather
  than an SPA route change) loses the login-granted confirmation and will still prompt; persisting it
  is deliberately not offered, because a reusable confirmation must not outlive the tab.

## [0.10.0] — 2026-07-27

Multi-audience support: an application that signs in more than one population (staff and customers, with
separate identity tables and overlapping ids) can now mount Omni2FA per population instead of squeezing
both through one shared mount. Driven by a live host that had to flag its portal's 2FA requests with a
custom header so the backend knew which cookie to read, and to prefix subjects by hand at login, at
finalize, and in a custom `IUserContextAccessor`. No protocol change — the paths under each mount are the
same, so OpenAPI stays at `0.8.0`.

### Added
- **Audiences (`Omni2FA.AspNetCore`)** — `AspNetCore.Audiences` declares a population as `Name` + `RoutePrefix` + `SubjectPrefix` + `AuthenticationSchemes`/`AuthorizationPolicy`. `MapOmni2Fa("customer")` mounts that audience's endpoints under its own prefix with its own scheme; `[Omni2FaAudience("name")]` / `.WithOmni2FaAudience("name")` tag the host's own endpoints (needed for `[RequireTwoFactor]`, which otherwise evaluates step-up against the default audience's subject). Mounting under the path that already identifies the population — e.g. `/api/portal/2fa` for a portal served from `/api/portal` — means the host's existing cookie/scheme/CORS rules cover the 2FA endpoints, so no request flag is needed.
- **Subject namespacing** — an audience's `SubjectPrefix` is applied by the library: mounts tag their endpoints, and the default `UserContextAccessor` prefixes the id from the principal (override `GetRawUserId()` to keep namespacing while reading the id from elsewhere). `IOmni2FaAudienceRegistry.ToSubject` / `TryGetUserId` apply the same rule where the host talks to Omni2FA outside a mount — issuing the pre-auth token at login, reading the subject back at finalize. `TryGetUserId` on the default audience rejects subjects carrying another audience's prefix.
- **Startup validation** — audience names, route prefixes, and subject prefixes must be unique, and every non-default audience must set a route prefix. `ValidateOnStart`, because a shared subject prefix silently merges two populations' 2FA methods.
- **`Omni2FaClientConfig.namespace` (`@omni2fa/core`)** — scopes the client's storage keys (`omni2fa:portal:preauth`). Two clients in one browser previously shared `omni2fa:preauth`, so with a shared `sessionStorage`/`localStorage` the second login would overwrite the first one's token. Explicit `preAuthStorageKey`/`sessionStorageKey` still win.

### Changed
- `MapOmni2Fa` takes an optional audience name and returns the mounted `RouteGroupBuilder` (was `IEndpointRouteBuilder`; source-compatible, `RouteGroupBuilder` implements it). Attach only conventions that are safe on `/challenge/*` — it runs before a session exists; per-audience scheme and policy belong in the audience options, which apply them solely to session-authenticated endpoints.
- The step-up `403 STEP_UP_REQUIRED` envelope now points at the caller's own audience mount (`stepUpPath`), not always the default one.
- Endpoint names are suffixed per audience past the default (`listMethods-customer`) — endpoint names are unique application-wide, so a second mount would otherwise fail at startup. The default audience keeps the bare names used as OpenAPI operation ids.
- `.NET` packages: version bump for the coordinated release; the JS packages' only change is `namespace`.

### Migration
- Nothing to do for a single-login host: the default audience is implicit, `MapOmni2Fa()` behaves as before, and existing subjects are stored unchanged.
- A host that already namespaces subjects by hand can move the prefix into an audience and delete its custom accessor — but the prefix string must stay byte-identical, since it is part of every stored subject.

## [0.9.0] — 2026-07-27

Frontend ergonomics release, driven by live host integration. Adding a single header to every 2FA request
used to mean replacing the whole transport via `fetch` — a wrapper that rebuilds the request from
`(url, init)` silently drops the headers and body `openapi-fetch` already put on the `Request` it passes.
Header decoration now has its own config slot, and the React step-up prompt registers itself. No protocol
change: OpenAPI stays at `0.8.0` and the .NET packages are a version-only bump to keep `major.minor`
aligned across the release (see the versioning model in `docs/ROADMAP.md`).

### Added
- **`Omni2FaClientConfig.headers` (`@omni2fa/core`)** — extra headers attached to every request, as a `HeadersInit` or a function resolved per request. Covers routing flags (`X-Portal-Auth`), a UI language the user can switch at runtime (`Accept-Language`), or an active-tenant id, without touching the transport. Applied before the client's own auth middleware, so a host-supplied `Authorization` still wins.
- **`useStepUp({ handleClientStepUp })` (`@omni2fa/react`)** — the hook now registers its `confirmTwoFactor` on the client while mounted, so the library's own step-up-gated calls (remove method, regenerate recovery codes, enroll start) prompt and retry without any host wiring. Default `true`; pass `false` when the host registers its own handler via `client.setStepUpHandler`.

### Changed
- `Omni2FaClientConfig.fetch` is documented as what it is — the escape hatch for *transport* (retry, logging, non-browser fetch), invoked with a ready-made `Request` that must be forwarded as-is. Use `headers` for headers.
- Hosts that call `omni.client.setStepUpHandler(confirmTwoFactor)` in an effect next to `useStepUp()` can delete it — the hook does it. Keep the manual call only if the handler isn't the one `useStepUp` returns, and then pass `{ handleClientStepUp: false }` so the two don't overwrite each other.
- `.NET` packages: version bump only, no code changes.

## [0.8.1] — 2026-06-11

Packaging/quality patch — **no API or behavior changes**. Cleans up the npm supply-chain footprint
reported by socket.dev.

### Fixed
- **`@omni2fa/core`** — removed the embedded `http://omni2fa.local` fallback-origin literal from the bundle. It was only ever used as a base for `new URL(path, origin)` to read a request's pathname (the origin was never fetched), but socket.dev's static analyzer flagged it as a *URL strings* supply-chain alert. Replaced with a pure string parser (`pathnameOf`) — byte-for-byte identical to `new URL(...).pathname` on all inputs, with no URL literal shipped.
- **Internal version alignment** — `@omni2fa/react` and `@omni2fa/react-mui` now depend on the matching `0.8.1` of `@omni2fa/core` / `@omni2fa/react` (they were left pinned to a stale `0.7.1`, so `@omni2fa/react@0.8.0` resolved an older core).

### Changed
- Every published npm package now ships a `LICENSE` file in its tarball (previously the MIT license lived only at the repo root, so it was absent from each package on npm).
- Added `socket.yml` documenting the reviewed-and-accepted dependency capabilities (network access via openapi-fetch, `process.env.NODE_ENV` in the react-ecosystem shims, minified UMD bundles in xstate/openapi-fetch).

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
