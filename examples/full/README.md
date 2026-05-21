# Omni2FA — Example app

End-to-end sandbox demonstrating the v0.1 library: register, sign in, enroll a TOTP authenticator, sign out, sign in again, verify with TOTP, manage methods.

## Prerequisites

- **Node.js** 20 or newer
- **.NET 8 SDK**

## Run

Open two terminals from the **repo root**.

**Terminal 1 — backend** (ASP.NET on `http://localhost:5000`):

```bash
cd examples/full/backend
dotnet run --urls http://localhost:5000
```

**Terminal 2 — frontend** (Vite dev server on `http://localhost:5173`):

```bash
cd examples/full/frontend/
npm install                                       # once
npm run dev                                       # builds @omni2fa/core + @omni2fa/react (predev), then starts the Vite dev server
```

Open `http://localhost:5173`.

> The `predev` hook builds the workspace libraries before each dev session. If you edit `@omni2fa/core` or `@omni2fa/react` *while* the dev server is running, rebuild them without restarting Vite by running `npm run build:libs` from `examples/full/frontend/`.

## What to try

1. **Register** an account on `/register` — you'll land on `/profile` with no 2FA enrolled.
2. Click **Add TOTP** in the *Two-factor authentication* card. Scan the QR with Google Authenticator / Authy / 1Password / Bitwarden. Submit the 6-digit code.
3. **Sign out**.
4. **Sign in** with the same email/password. You'll be redirected to `/2fa`. Pick the method, enter the current code from your authenticator, submit.
5. Back on `/profile` — verified through TOTP, host session JWT issued by `/auth/finalize`.
6. Remove the method via the trash-can icon — your next sign in will skip the 2FA step.

## What's where

```
examples/full/
├── backend/                      ASP.NET Core 8, in-memory EF, controllers + Minimal API endpoints from Omni2FA
│   ├── Controllers/              AuthController + UserController
│   ├── Services/                 AuthService, PasswordHasher, HostSessionIssuer
│   ├── Entities/User.cs          host's user table
│   ├── Dtos/Auth/                LoginRequest/Response, TwoFactorChallengeResponse, …
│   ├── AppDbContext.cs           ApplyOmni2FaConfiguration() wires the 2FA tables
│   ├── Program.cs                AddOmni2Fa + MapOmni2Fa + JwtBearer for host session
│   └── appsettings.json          dev keys
└── frontend/                     Vite + React 19 + MUI v6
    ├── src/
    │   ├── api/authClient.ts     host's own auth fetchers (NOT part of Omni2FA)
    │   ├── auth/                 AuthContext holding the host session JWT
    │   ├── pages/                LoginPage, RegisterPage, TwoFactorChallengePage, ProfilePage
    │   ├── components/           TwoFactorSection, AddTotpDialog, ProtectedRoute
    │   ├── omni2fa.ts            createOmni2Fa({ baseUrl: '/api/2fa' }) singleton
    │   └── main.tsx              ThemeProvider + Omni2FaProvider + AuthProvider
    └── vite.config.ts            proxies /auth, /user, /api to the backend
```

## Notes for v0.5

`TwoFactorSection.tsx` and `AddTotpDialog.tsx` are hand-rolled here against `@omni2fa/react` headless hooks. When `@omni2fa/react-mui` ships (v0.5 milestone), drop them and import the styled equivalents directly.
