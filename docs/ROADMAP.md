# Omni2FA — Roadmap

## Versioning model: coordinated minor/major, independent patches

All publishable packages share the same **major.minor** version. They are released together under a single library milestone (e.g. `v0.3.0`). **Patches** (`x.y.Z`) ship independently per package — a bugfix in `@omni2fa/react-mui` doesn't bump anything else.

**Compatibility rule** (no matrix needed):
- During `0.x` — packages on the same `0.MAJOR.*` are guaranteed compatible. Mixing `0.3.x` and `0.2.x` is unsupported.
- From `1.0` onward — packages on the same `MAJOR.*.*` are compatible (standard semver).

**Examples**

✅ `@omni2fa/core@0.3.0` + `@omni2fa/react@0.3.4` + `Omni2FA.Core@0.3.1` → all on 0.3, compatible.
❌ `@omni2fa/core@0.3.0` + `@omni2fa/react@0.2.7` → different minors during 0.x, unsupported.
✅ `@omni2fa/core@1.2.0` + `@omni2fa/react@1.5.3` + `Omni2FA.Core@1.0.1` → all on 1.x, compatible.

This is the same model used by `Microsoft.AspNetCore.*`, NestJS, and Nx. No compatibility matrix to maintain; users just align major.minor.

The table below tracks the **library milestones** — when each capability lands across all packages, released as a single coordinated `vMAJOR.MINOR.0` tag.

Status legend: ⬜ planned · 🟦 in progress · ✅ shipped

---

## v0.x — pre-alpha (current)

The goal is one method working end-to-end across .NET + React + EF before broadening.

| Version | Capability | Scope | Status |
|---------|-----------|-------|--------|
| **v0.1** | TOTP end-to-end | `.NET endpoints + EF store + React headless hooks. Enroll TOTP, login with TOTP. Pre-auth token issued.` | ⬜ |
| **v0.2** | Email OTP | `Built-in SMTP sender (MailKit) + templated emails + i18n scaffold (en at minimum). Pluggable IEmailSender for users with their own infra.` | ⬜ |
| **v0.3** | WebAuthn | `Passkeys + hardware keys via Fido2NetLib + @simplewebauthn/*. Multiple credentials per user (configurable cap).` | ⬜ |
| **v0.4** | Recovery codes | `Hash-stored, generated on first method enrollment, regeneration endpoint, one-time use, shown once. Replaces 2FA on login.` | ⬜ |
| **v0.5** | Ready UI (`@omni2fa/react-mui`) | `Drop-in dialogs ported from the QRpark reference — TwoFactorSection, enrollment dialogs per method, regenerate dialog. MUI styled, themeable.` | ⬜ |
| **v0.6** | Stabilization | `Rate limiting hardening (default 20 attempts/min/IP), audit sink interface finalized, error code catalogue locked, OpenAPI 1.0 frozen.` | ⬜ |

## v1.0 — first public release

| Version | Capability | Status |
|---------|-----------|--------|
| **v1.0** | All v0.x methods + full docs + e2e example + multi-locale UI strings + published to npm/NuGet under stable channel. | ⬜ |

## v1.x — quality of life

| Version | Capability | Notes | Status |
|---------|-----------|-------|--------|
| **v1.1** | Trusted devices ("remember this browser") | Opt-in. Bound to user-agent + cookie + server-side device record. Configurable TTL. | ⬜ |
| **v1.2** | Angular package | `@omni2fa/angular` — same headless logic from `@omni2fa/core/js`, Angular components wrapping it. | ⬜ |

## v2.0+ — if demand justifies it

- Python backend (`omni2fa-python`) — same OpenAPI contract, FastAPI/Starlette adapter.
- SMS OTP — pluggable sender (Twilio, MessageBird, internal SMPP). Open question: provider auto-rotation.
- Push notifications via FCM / APNs — depends on app having mobile presence.

---

## Explicitly out of scope

These are **not** going into Omni2FA, even later. They belong to the host application:

- **Account recovery when a user loses everything** (methods + recovery codes). This is a business decision — support ticket, admin override, trusted contact, identity proofing. Omni2FA only provides the primitive (admin can call "reset all 2FA for user X" via the API), the *policy* around it is yours.
- **Password authentication itself.** Omni2FA layers on top of your existing login. You verify the password; we handle everything after.
- **Session management / JWT issuance.** We return "verified, here is the user id" — your app mints its session.
- **User management UI.** Profile/settings pages are yours; we only provide the 2FA section.

---

## Cross-stack guarantees (binding from v1.0)

- The OpenAPI contract in `Core/protocol/` is the source of truth. Its version is the library version — `Core/protocol/omni2fa.openapi.yaml` declares `info.version: x.y.0` matching the library milestone.
- Contract-breaking changes bump major. Field additions / new optional endpoints bump minor.
- Any backend that implements the contract at version `X.Y` works with any frontend that implements the contract at version `X.Y` (or any patch thereof).
