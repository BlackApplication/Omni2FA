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

The goal is one method working end-to-end across .NET + React + EF before broadening. Every `v0.x` ships an updated working **example** in `examples/full/` — see "Examples roadmap" below.

| Version | Capability | Scope | Status |
|---------|-----------|-------|--------|
| **v0.1** | TOTP end-to-end | `.NET endpoints + EF store + React headless hooks. Enroll TOTP, login with TOTP. Pre-auth token issued.` | ✅ |
| **v0.2** | Email OTP | `Built-in SMTP sender (MailKit) + templated emails + i18n scaffold (en at minimum). Pluggable IEmailSender for users with their own infra.` | ✅ |
| **v0.3** | WebAuthn | `Passkeys + hardware keys via Fido2NetLib + native browser marshaling. Multiple credentials per user (configurable cap).` | ✅ |
| **v0.4** | Recovery codes | `Hash-stored, generated on first method enrollment, regeneration endpoint, one-time use, shown once. Replaces 2FA on login.` | ✅ |
| **v0.5** | Ready UI (`@omni2fa/react-mui`) | `Drop-in dialogs — TwoFactorSection, enrollment dialogs per method, regenerate dialog. MUI styled, themeable.` | ⬜ (deferred — hosts use headless `@omni2fa/react` for now) |
| **v0.6** | Stabilization | `Rate limiting (default 20 attempts/min/IP), audit sink (IOmni2FaAuditSink), session-token client API (setSessionToken/getSessionToken + URL-based token routing), last-method policy. Shipped together with recovery codes.` | ✅ |

## v1.0 — first public release

| Version | Capability | Status |
|---------|-----------|--------|
| **v1.0** | All v0.x methods + full docs + e2e example + multi-locale UI strings + published to npm/NuGet under stable channel. | ⬜ |

## v1.x — broaden the ecosystem

Angular is part of v1.0 alongside React, so the cross-stack claim is real at first public release (two officially supported frontends, one backend). v1.1 and v1.2 then add the two largest backend ecosystems (Node.js, Python) before any other expansion.

| Version | Capability | Notes | Status |
|---------|-----------|-------|--------|
| **v1.1** | **Node.js / TypeScript backend** (`@omni2fa/node`) | Express, Fastify, NestJS adapters. Reuses `@omni2fa/core` for shared types and validation — same package powers Node bek and JS frontend, unique to this stack. | ⬜ |
| **v1.2** | **Python backend** (`omni2fa-python`) | FastAPI primary, Django/Flask via thin wrappers. Implements the same OpenAPI contract as .NET. | ⬜ |
| **v1.3** | Trusted devices ("remember this browser") | Opt-in. Bound to user-agent + cookie + server-side device record. Configurable TTL. | ⬜ |
| **v1.4** | Vue package (`@omni2fa/vue`) | Headless composables wrapping `@omni2fa/core`. | ⬜ |
| **v1.5** | `@omni2fa/react-tailwind` | Drop-in 2FA UI on Tailwind + Headless UI. Mirrors `@omni2fa/react-mui` feature-for-feature. Validates the headless core isn't MUI-shaped. | ⬜ |
| **v1.6** | Pluggable pre-auth transport | Opt-in cookie transport (`Set-Cookie: HttpOnly; Secure; SameSite`) alongside the default Bearer header. Host picks via `o.PreAuth.Transport`. Every backend adapter implements both; OpenAPI declares both `bearerAuth` and `cookieAuth` security schemes as parallel options. Recommended when the host's main session is also cookie-based. | ⬜ |

## v2.0+ — community-driven extras

Things explicitly **not** done by the Omni2FA team — open to community PRs via [`docs/PORTING_GUIDE.md`](PORTING_GUIDE.md):

- Java / Kotlin Spring Boot backend.
- Go backend.
- PHP / Ruby / Rust / Elixir backends.
- Svelte / Solid frontend adapters.

Things on the official roadmap but demand-gated:

- SMS OTP — pluggable sender (Twilio, MessageBird, internal SMPP). Open question: provider auto-rotation.
- Push notifications via FCM / APNs — depends on app having mobile presence.

---

## Deferred — discovered during v0.1, scheduled for later

Polish items that surfaced while shipping v0.1 and have a clear target version. Recorded here so they don't get lost.

### Host session-token client API — target **v0.6**

**Problem.** `Omni2FaClient` currently knows only about its own pre-auth token (`setPreAuthToken` / `getPreAuthToken`). For host-session endpoints (`/methods`, `/enroll/*`) the host must attach its own session credential — today that means writing ~15-25 lines of custom `fetch` wrapper. See `examples/full/frontend/src/omni2fa.ts` for the workaround.

**Design principle.** Three host-auth styles exist in the wild; the client must support all of them without forcing one shape:

| Host style | How they store session | What we expose |
|---|---|---|
| Bearer JWT in JS (most common) | `localStorage` / `sessionStorage` / in-memory React state | **Sugar:** `setSessionToken()` / `clearSessionToken()` / `getSessionToken()` |
| HttpOnly cookie (the "no-XSS-leak" school) | Cookie set by host, invisible to JS | **Sugar:** new config `credentials: 'include'`. The browser attaches the cookie; we just opt in |
| SSO, signing, multi-tenant routing, custom headers | Anything else | **Escape hatch:** existing `fetch` config option. The host hands us a `fetch` that does whatever it needs — already supported |

The Bearer sugar is just ergonomic shorthand over the same `fetch` slot. Removing `fetch` would be a regression — keep it as the universal escape hatch.

**Proposed API.**

```ts
export interface Omni2FaClientConfig {
    baseUrl: string;
    storage?: IStorage;
    fetch?: typeof fetch;                              // escape hatch — unchanged
    credentials?: 'omit' | 'same-origin' | 'include'; // NEW — for cookie-based auth
    preAuthStorageKey?: string;
    sessionStorageKey?: string;                        // NEW — symmetric with preAuthStorageKey
}

export interface IOmni2FaClient {
    setPreAuthToken(token: string | null): void;       // existing
    getPreAuthToken(): string | null;

    setSessionToken(token: string | null): void;       // NEW
    getSessionToken(): string | null;

    // ... endpoint methods ...
}
```

**Internal token routing.** Existing pre-auth middleware blindly attaches the pre-auth token. Change it to pick the right token per request URL:

```ts
onRequest: ({ request }) => {
    if (request.headers.has('Authorization')) return request;  // host's custom fetch wins
    const isPreAuthEndpoint = request.url.includes('/challenge/');
    const token = isPreAuthEndpoint ? this.getPreAuthToken() : this.getSessionToken();
    if (token) request.headers.set('Authorization', `Bearer ${token}`);
    return request;
}
```

URL string-match is sufficient — endpoint paths are frozen in the OpenAPI contract.

**Then the example shrinks to:**

```ts
// omni2fa.ts
export const omni = createOmni2Fa({ baseUrl: '/api/2fa' });

// AuthContext setSession:
omni.client.setSessionToken(session?.sessionToken ?? null);
```

Custom `fetch` and localStorage-reader removed. Frontend integration becomes drop-in for the common case while staying maximally flexible for the rest.

**Why not now.** Adds public API surface (two new methods + two new config fields); want to ship it together with rate-limit / audit / error-code lock-in so v0.6 is one coordinated stabilization release before v1.0 freeze.

---

## Explicitly out of scope

These are **not** going into Omni2FA, even later. They belong to the host application:

- **Account recovery when a user loses everything** (methods + recovery codes). This is a business decision — support ticket, admin override, trusted contact, identity proofing. Omni2FA only provides the primitive (admin can call "reset all 2FA for user X" via the API), the *policy* around it is yours.
- **Password authentication itself.** Omni2FA layers on top of your existing login. You verify the password; we handle everything after.
- **Session management / JWT issuance.** We return "verified, here is the user id" — your app mints its session.
- **User management UI.** Profile/settings pages are yours; we only provide the 2FA section.

---

## Examples roadmap

A single growing reference app lives at `examples/full/`. It is **updated with every v0.x release** so the example always demonstrates the latest capability set.

```
examples/
└── full/
    ├── backend/         ASP.NET Core minimal API + EF Core + SQLite (Postgres on v1.0)
    │   ├── Program.cs
    │   ├── AppDbContext.cs
    │   └── appsettings.json
    └── frontend/        Vite + React + MUI (raw headless on v0.1-v0.4, switches to react-mui at v0.5)
        ├── src/
        └── package.json
```

| Version | Example state |
|---------|---------------|
| **v0.1** | Skeleton + TOTP enroll/login. Run with `dotnet run` + `npm run dev`. SQLite, no external services. **This is the first sandbox you can poke.** |
| **v0.2** | + Email OTP. Local SMTP catcher (Mailpit/Papercut on `localhost:1025`) for testing emails. SQLite replaces InMemory. ✅ |
| **v0.3** | + WebAuthn enrollment & login. Works on localhost without HTTPS (per spec). Any non-localhost origin needs TLS — see `docs/FLOWS.md` → "Common deployment gotchas". ✅ |
| **v0.4** | + Recovery codes UX. Generation modal, use-on-login flow. ✅ |
| **v0.5** | Frontend swaps raw headless usage for `@omni2fa/react-mui` ready dialogs. Same backend. (deferred) |
| **v0.6** | + audit (default logger), rate limit enforced, session-token client API (custom fetch removed). ✅ |
| **v1.0** | Production-like: docker-compose with Postgres, i18n (en + ru), screenshots, optional video walkthrough, README polish. |

Post-v1.0, when additional UI packages (Tailwind, Angular) ship, we'll likely fork to a few variants like `examples/full-tailwind/`, `examples/full-angular/`. Until then — single example, single source of truth.

## Cross-stack guarantees (binding from v1.0)

- The OpenAPI contract in `Core/protocol/` is the source of truth. Its version is the library version — `Core/protocol/omni2fa.openapi.yaml` declares `info.version: x.y.0` matching the library milestone.
- Contract-breaking changes bump major. Field additions / new optional endpoints bump minor.
- Any backend that implements the contract at version `X.Y` works with any frontend that implements the contract at version `X.Y` (or any patch thereof).
