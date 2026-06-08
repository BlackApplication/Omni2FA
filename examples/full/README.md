# Omni2FA — Example app

End-to-end sandbox for the Omni2FA library. A single growing reference app — updated with every
`v0.x` release. As of **v0.3** it demonstrates: register, sign in, enroll a **TOTP** authenticator,
an **Email OTP** method, or a **WebAuthn passkey / security key**, sign out, sign back in and verify
with any enrolled method, manage methods.

> This is **one** full example (`examples/full`) — host backend (ASP.NET Core) + host frontend
> (React) wiring Omni2FA in. It is the same app across versions.

## Prerequisites

- **Node.js** 20 or newer
- **.NET 8 SDK**
- **A local SMTP catcher** — only needed to receive Email OTP codes. [Mailpit](https://github.com/axllent/mailpit) is the easy default:
  ```bash
  docker run -d -p 1025:1025 -p 8025:8025 axllent/mailpit
  ```
  SMTP listens on `localhost:1025` (matches `appsettings.json`); the web inbox is at `http://localhost:8025`.
  Without it, TOTP still works fully — only Email enrollment/login needs a place for the code to land.

## Run

Open two terminals from the **repo root**.

**Terminal 1 — backend** (ASP.NET on `http://localhost:5000`):

```bash
cd examples/full/backend
dotnet run --urls http://localhost:5000
```

On first run it creates a local **SQLite** file `omni2fa-example.db` next to the project (gitignored).
Delete it to reset all users and 2FA state.

**Terminal 2 — frontend** (Vite dev server on `http://localhost:5173`):

```bash
cd examples/full/frontend/
npm install                                       # once
npm run dev                                       # builds @omni2fa/core + @omni2fa/react (predev), then starts the Vite dev server
```

Open `http://localhost:5173`.

> The `predev` hook builds the workspace libraries before each dev session. If you edit `@omni2fa/core` or `@omni2fa/react` *while* the dev server is running, rebuild them without restarting Vite by running `npm run build:libs` from `examples/full/frontend/`.

## What to try

### TOTP (authenticator app)

1. **Register** an account on `/register` — you'll land on `/profile` with no 2FA enrolled.
2. Click **Add TOTP** in the *Two-factor authentication* card. Scan the QR with Google Authenticator / Authy / 1Password / Bitwarden. Submit the 6-digit code.
3. **Sign out**, then **sign in** again. You'll be redirected to `/2fa`. Pick the TOTP method, enter the current code, submit.

### Email OTP

4. On `/profile`, click **Add Email**. Enter any address (it doesn't have to be real — the code lands in Mailpit). A code is emailed; open `http://localhost:8025`, copy it, confirm. Use **Resend code** if the cooldown has passed.
5. **Sign out**, **sign in** again, and on `/2fa` pick the **Email** method. A fresh code is sent — grab it from Mailpit and verify. **Resend code** is available there too.

### WebAuthn (passkey / security key)

6. On `/profile`, click **Add Passkey**. Your browser/OS prompts (Touch ID, Windows Hello, a security key, or a synced passkey). Approve it — the credential is registered. You can add up to 3.
7. **Sign out**, **sign in** again, pick the **passkey** method on `/2fa`, and approve the browser prompt — no code to type. The signature counter is updated server-side each login.

> WebAuthn works on `localhost` without HTTPS. On any other origin it requires TLS and the
> `Omni2Fa:WebAuthn` `RelyingPartyId`/`Origins` must match the real hostname — see
> `docs/FLOWS.md` → "Common deployment gotchas".

### Recovery codes

8. When you enroll your **first** method, a set of one-time **recovery codes** is shown — save them. They're hashed at rest and shown only once.
9. **Regenerate recovery codes** from the 2FA card to invalidate the old set and get a fresh one.
10. At login, click **Use a recovery code instead** on `/2fa` and enter one — it logs you in and is consumed (one-time).

### Manage

11. Back on `/profile` — verified, host session JWT issued by `/auth/finalize` (which validates the verified-handoff token from the verify step).
12. Remove a method via the trash-can icon. With no methods left, recovery codes are wiped and the next sign in skips the 2FA step.

> **Who owns what:** the host verifies the password and issues the final session JWT; Omni2FA issues
> the short-lived pre-auth token and runs the 2FA ceremony. The **email address is supplied by the
> host** in the enroll request — Omni2FA stores it on the method and sends there at login, but never
> derives it from a claim or verifies ownership (host policy).

## What's where

```
examples/full/
├── backend/                      ASP.NET Core 8, EF Core + SQLite, controllers + Minimal API endpoints from Omni2FA
│   ├── Controllers/              AuthController + UserController
│   ├── Services/                 AuthService, PasswordHasher, HostSessionIssuer
│   ├── Entities/User.cs          host's user table
│   ├── Dtos/Auth/                LoginRequest/Response, TwoFactorChallengeResponse, …
│   ├── AppDbContext.cs           ApplyOmni2FaConfiguration() wires the 2FA tables
│   ├── Program.cs                AddOmni2Fa + MapOmni2Fa + JwtBearer for host session; SQLite + EnsureCreated
│   └── appsettings.json          dev keys + Omni2Fa:Email SMTP (localhost:1025) + Omni2Fa:WebAuthn (localhost)
└── frontend/                     Vite + React 19 + MUI v6
    ├── src/
    │   ├── api/authClient.ts     host's own auth fetchers (NOT part of Omni2FA)
    │   ├── auth/                 AuthContext holding the host session JWT
    │   ├── pages/                LoginPage, RegisterPage, TwoFactorChallengePage, ProfilePage
    │   ├── components/           TwoFactorSection, AddTotpDialog, AddEmailDialog, AddWebAuthnDialog, ProtectedRoute
    │   ├── omni2fa.ts            createOmni2Fa({ baseUrl: '/api/2fa' }) singleton
    │   └── main.tsx              ThemeProvider + Omni2FaProvider + AuthProvider
    └── vite.config.ts            proxies /auth, /user, /api to the backend
```

## Configuring email (and using your own sender)

The example uses the built-in MailKit SMTP sender, configured under `Omni2Fa:Email` in
`appsettings.json` — point `Smtp` at any server (set `Username`/`Password`/`UseStartTls` for a real
one; supply secrets via env vars or user-secrets, not the file). To send through your **own** email
infrastructure instead, register an `IEmailSender` before `AddOmni2Fa(...)` and Omni2FA uses it,
ignoring the SMTP config entirely.

By default codes are sent on a **background worker** (`Omni2Fa:Email:BackgroundDelivery`, on) so the
endpoints return instantly and SMTP latency/failures don't block the user — delivery errors are
logged. Set it to `false` to send inline (awaited) if you'd rather have SMTP errors surface to the
caller.

## Production hardening (v0.6)

Enabled by default in the library — visible in this example:

- **Rate limiting** — sensitive endpoints (challenge verify/resend/recovery-code, enroll groups) are capped at 20 attempts/min/IP. Exceeding it returns `429` with `Retry-After`. Tune via `Omni2Fa:RateLimit` (`Enabled`, `PermitLimit`, `Window`).
- **Audit** — every enroll/remove/verify/recovery/rate-limit event is logged (default `IOmni2FaAuditSink` → `ILogger`; watch the backend console). Register your own sink to forward to a DB/SIEM.
- **Session-token client API** — `omni2fa.ts` no longer needs a custom `fetch`: `AuthContext` calls `omni.client.setSessionToken(...)`, and the client routes the pre-auth token to `/challenge/*` and the session token to host-session endpoints automatically.
- **Last-method policy** — set `Omni2Fa:AspNetCore:AllowDisablingLastMethod` to `false` to forbid removing the user's last method (`409 LAST_METHOD_PROTECTED`).

> Schema changed across versions (recovery-code table, email/webauthn columns). The example uses
> `EnsureCreated()` (no migrations), so after pulling a new version delete `omni2fa-example.db` to
> recreate the schema.

## Notes for v0.5

`TwoFactorSection.tsx`, `AddTotpDialog.tsx`, `AddEmailDialog.tsx`, and `AddWebAuthnDialog.tsx` are
hand-rolled here against `@omni2fa/react` headless hooks. When `@omni2fa/react-mui` ships (v0.5
milestone), drop them and import the styled equivalents directly.
