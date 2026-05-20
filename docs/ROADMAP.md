# Omni2FA — Roadmap

Versioning is **independent per package** (no monorepo-wide lockstep). The table below tracks the **overall library milestones** — when each capability lands across the .NET and React packages.

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

- The OpenAPI contract in `Core/protocol/` is the source of truth.
- Backwards-compatible breaks bump major. Field additions are minor.
- Any backend that implements the contract works with any frontend that implements the contract.
