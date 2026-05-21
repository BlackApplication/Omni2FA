# Changelog

All notable changes to Omni2FA will be documented in this file. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

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
