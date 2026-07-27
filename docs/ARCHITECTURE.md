# Omni2FA — Architecture

This document is **binding** for all code in this repository. Read it before opening a PR or starting on a new package. It exists to prevent the single mistake that would make Omni2FA expensive to maintain: leaking framework-specific logic into core, or business logic into framework adapters.

---

## 1. Core principle

> **Framework-agnostic TS/C# cores. Thin framework-specific adapters.**

The same rule applies on both sides of the stack:

| Stack | Framework-agnostic core | Adapters |
|-------|-------------------------|----------|
| JavaScript | `@omni2fa/core` (`Core/js/`) | `@omni2fa/react`, `@omni2fa/react-mui` (`React/*`), future `@omni2fa/vue`, `@omni2fa/angular`, `@omni2fa/svelte`… |
| .NET | `Omni2FA.Core` (`.Net/Core/`) | `Omni2FA.AspNetCore`, `Omni2FA.AspNetCore.EntityFrameworkCore` (`.Net/src/`) |

Realistic split target: **~80% of UI-related logic lives in core**, ~20% is unavoidable framework-specific reactivity/lifecycle glue. We will not pretend the adapter is "just rendering" — but we will pretend it is **stateless logic-wise** and enforce that ruthlessly.

---

## 2. Boundary map (JavaScript side)

### Lives in `@omni2fa/core`

| Concern | Notes |
|---------|-------|
| **Login state machine** | Discriminated union: `Idle → PasswordSent → AwaitingMethodPick → ChallengeIssued → AwaitingVerify → Verified \| Failed`. Pure TS, no React/Vue imports. |
| **Enrollment state machines** | One FSM per method type (TOTP, Email, WebAuthn, RecoveryCodes). |
| **WebAuthn marshaling** | Encode/decode challenge bytes (base64url ↔ `ArrayBuffer`), invoke `navigator.credentials.create() / .get()`. Browser APIs are not framework-specific. |
| **HTTP client** | Typed wrapper over `fetch`. Generated from OpenAPI types. Maps HTTP errors to error codes (rule 9 in CODE_STYLE.md). |
| **Token lifecycle** | Pre-auth token storage (via `IStorage`), expiry tracking, auto-invalidation on verify or expire. |
| **Timers** | TOTP 30s window, email resend cooldown, pre-auth token countdown. Plain `setInterval` / `setTimeout`. |
| **Error code → message mapping** | i18n-aware: `mapError(code, locale)`. |
| **Validation** | `validateOtpFormat(input)`, `validateRecoveryCode(input)`. Pure functions. |
| **Recovery code formatting** | Split into chunks of 4, generate downloadable text blob, copy-to-clipboard helper. |
| **Storage abstraction** | `IStorage` interface (`get`, `set`, `remove`). Default implementations: `MemoryStorage`, `LocalStorageStorage`, `SessionStorageStorage`. |

### Lives in framework packages (`@omni2fa/react`, future Vue/Angular/etc.)

| Concern | Notes |
|---------|-------|
| **Reactivity binding** | React → `useSyncExternalStore`. Vue → `customRef` / `shallowRef`. Angular → `toSignal()` or RxJS `Observable`. |
| **Lifecycle** | `useEffect` cleanup to unsubscribe from stores and stop timers on unmount. Vue `onUnmounted`, Angular `OnDestroy`. |
| **Rendering** | JSX / SFC / templates. |
| **DI / context** | `<Omni2FaProvider>` for React, `provide/inject` for Vue, root service for Angular — all wrap **one** core instance and pass it down. |
| **Form integration** | Forwarded to host app's form library — we accept controlled values, never own form state. |

### Lives in styled packages (`@omni2fa/react-mui`, future `@omni2fa/react-tailwind`)

| Concern | Notes |
|---------|-------|
| **Visual components** | Pre-built dialogs/sections styled with the chosen design system. |
| **Theme integration** | Reading theme from MUI ThemeProvider / Tailwind classes. |
| **Localization passthrough** | UI strings → host's i18n library (`react-i18next`, etc.) — we don't bundle translations, only provide keys. |

**Nothing else.** A styled package is a child of an adapter package — it imports from `@omni2fa/react` and **never** from `@omni2fa/core` directly.

#### Customization is binding (styled packages must be adaptable, not take-it-or-leave-it)

A drop-in styled component must still bend to the host's ecosystem without forking it. Every styled
package is built on three principles:

1. **Composable, not monolithic.** Export the small pieces (`MethodList`, `AddMethodMenu`, `TotpEnrollDialog`, `EmailEnrollDialog`, `WebAuthnEnrollDialog`, `RecoveryCodesView`, `LoginChallenge`) as first-class components. The big convenience component (`TwoFactorSection`) is **just a default composition of those pieces** — a host that wants a different layout assembles the pieces itself.
2. **Every part is overridable via slots.** Each component takes `slots` / `slotProps` (or render-props) so any sub-element can be swapped without forking. Plus `sx` / `className` passthrough on the root and a theme that is read from the host (MUI `ThemeProvider`, Tailwind config) — never hardcoded colors.
3. **No baked-in copy.** All user-facing text comes from props or an i18n key map (host's `react-i18next` etc.), keyed by stable error/UI codes. Behavior seams (`onEnrolled`, `onError`, `onMethodRemoved`, controlled values) are props, not internal state.

The universal escape hatch remains the headless `@omni2fa/react` hooks: a host that rejects the styled
layer entirely renders its own UI against `useChallenge()` / `useMethods()` / `useXxxEnrollment()`. The
three tiers — **hooks (logic) → optional headless-structural components (structure + a11y, no styles) →
thin styled skins (`react-mui`, `react-tailwind`)** — are how "drop-in for the lazy, fully customizable
for the rest" is delivered. Building a styled package as a non-overridable monolith is a bug.

---

## 3. Adapter contract: subscribe / getSnapshot

Core exposes per-flow **stores** with exactly this shape:

```ts
interface Store<TState> {
    subscribe(callback: () => void): () => void;  // returns unsubscribe
    getSnapshot(): TState;                          // returns current immutable state
    // Action methods are flow-specific: store.startEnroll(), store.submitCode(...), etc.
}
```

This is the same shape React's `useSyncExternalStore` consumes natively, and it's trivial to wrap for Vue (`customRef`) or Angular (`toSignal()`). No core code depends on any framework.

**React adapter example:**

```ts
// @omni2fa/react/src/useLoginFlow.ts
export function useLoginFlow() {
    const store = useOmni2FaContext().loginStore;
    return useSyncExternalStore(store.subscribe, store.getSnapshot);
}
```

That's the entire bridge. The hook is **stateless and logic-free** — all state, transitions, and effects happen inside `loginStore`.

**Vue adapter example (future v1.x):**

```ts
// @omni2fa/vue/src/useLoginFlow.ts
export function useLoginFlow() {
    const store = useOmni2Fa().loginStore;
    const state = shallowRef(store.getSnapshot());
    const unsubscribe = store.subscribe(() => { state.value = store.getSnapshot(); });
    onScopeDispose(unsubscribe);
    return state;
}
```

Same five lines per fra­mework, no business logic.

---

## 4. Forbidden in framework / styled packages

If any of these appear in `@omni2fa/react`, `@omni2fa/react-mui`, or future Vue/Angular packages — it's a bug, move it to core.

- ❌ `if (method.type === 'Totp') { ... }` — type-specific business branching. Belongs to FSM in core.
- ❌ `setTimeout(..., 30_000)` for TOTP window. Belongs to TOTP timer in core.
- ❌ String parsing/formatting of OTPs, recovery codes, base32 secrets. Belongs to helpers in core.
- ❌ `fetch('/api/2fa/...')` direct calls. Belongs to HTTP client in core.
- ❌ Hashing, encoding, base64url conversions. Belongs to core.
- ❌ Validation regexes or rules. Belongs to `validateXxx` in core.
- ❌ Error code translation (`'INVALID_CODE' → 'Wrong code'`). Belongs to `mapError` in core.
- ❌ Mutable local state that survives re-render. State lives in stores, hooks only read snapshots.

**Allowed in framework packages:**

- ✅ Reading from a store via the subscribe/getSnapshot contract.
- ✅ Calling action methods on a store (`store.submitCode(value)`).
- ✅ `useEffect` for mount/unmount lifecycle.
- ✅ Rendering JSX based on snapshot.
- ✅ Passing controlled values from host's forms into store actions.

---

## 5. Code review checklist

Before merging anything in `React/` (or future `Vue/`, `Angular/`):

- [ ] Does this file import from `react` / `vue` / `@angular/core`?
  - If yes → it must be in the framework package, not core.
- [ ] Does this file import from `@omni2fa/core` only (no other framework)?
  - If `@omni2fa/react-mui` imports `vue` — that's a bug.
- [ ] Are there any `if (type === '...')` branches, timers, regexes, or `fetch` calls?
  - If yes → those moves to core, leaving the framework file as a thin subscriber.
- [ ] Does the component own any state that isn't `useSyncExternalStore` output?
  - If yes — challenge whether it should be in core.

For the .NET side (`Omni2FA.AspNetCore.*`):

- [ ] Does this file import `Microsoft.EntityFrameworkCore` outside `Omni2FA.AspNetCore.EntityFrameworkCore`?
  - If yes → bug, business logic must not couple to a specific store.
- [ ] Does `Omni2FA.Core` reference `Microsoft.AspNetCore.*` packages?
  - If yes → bug, core stays framework-agnostic.

---

## 6. Cross-package dependency graph

### JavaScript

```
@omni2fa/core          (NO peer deps on any UI framework)
    ↑
    │   imports allowed
    │
@omni2fa/react         (peer: react)
    ↑
@omni2fa/react-mui     (peer: react, @mui/material)

@omni2fa/core
    ↑
@omni2fa/vue           (peer: vue)              [future v1.x]

@omni2fa/core
    ↑
@omni2fa/angular       (peer: @angular/core)    [future v1.x]
```

Rules:
- Every adapter imports from `@omni2fa/core` and from **its own** UI framework only.
- Styled packages import from their adapter (`@omni2fa/react-mui` from `@omni2fa/react`), never from core directly.
- Adapters never import from each other (`@omni2fa/react` cannot import from `@omni2fa/vue`).

### .NET

Physical layout inside `.Net/` reflects the core/adapter boundary directly:

```
.Net/
├── Omni2FA.sln
├── Core/                                  ← framework-agnostic backbone
│   └── Omni2FA.Core/
└── src/                                   ← ASP.NET-specific adapters
    ├── Omni2FA.AspNetCore/
    └── Omni2FA.AspNetCore.EntityFrameworkCore/
```

Project dependency graph:

```
                Omni2FA.Core                                    (in .Net/Core/ — no AspNetCore, no EF, no HTTP, no Fido2)
                    ↑
        ┌───────────┴───────────────────────────────┐
        │                                           │
Omni2FA.AspNetCore                             Omni2FA.AspNetCore.EntityFrameworkCore
(in .Net/src/ — Core + ASP.NET +               (in .Net/src/ — Core + EF Core only;
 Fido2NetLib + MailKit)                         no AspNetCore reference)
```

`Omni2FA.AspNetCore.EntityFrameworkCore` only implements the `Omni2FA.Core` store interfaces with EF Core. It does **not** depend on `Omni2FA.AspNetCore` — hosts that use a custom HTTP layer (Worker Service, gRPC, minimal API rolled by hand) can still pull in just the EF adapter.

**WebAuthn is part of the ASP.NET adapter, not a separate package.** `Omni2FA.Core` defines the FIDO2-free `IWebAuthnCeremonyService` contract; the Fido2NetLib implementation lives inside `Omni2FA.AspNetCore`. Earlier drafts had a standalone `Omni2FA.WebAuthn` package, but WebAuthn is a first-class 2FA method that every ASP.NET host gets anyway — a separate package added a publish/version unit for no real consumer benefit (the core stays Fido2-free either way). Folded in for simplicity; a host that doesn't want passkeys simply doesn't enrol them.

Rules:
- `Omni2FA.Core` lives in **`.Net/Core/`**. It references no ASP.NET, no EF, no HTTP, no Fido2 — pure domain + interfaces + standards-based crypto (TOTP).
- `.Net/src/` holds **adapter packages** — store implementations, HTTP-layer packages, etc. A project lives in `src/` if it pulls in any infrastructure dependency (EF Core, ASP.NET Core, a specific HTTP client).
- Adapters are **independent siblings** — `Omni2FA.AspNetCore.EntityFrameworkCore` does NOT depend on `Omni2FA.AspNetCore`. Each adapter pulls only `Omni2FA.Core` plus its own infrastructure SDK.
- `Omni2FA.sln` lives in `.Net/` root and references projects from both `Core/` and `src/`.
- A future `Omni2FA.Dapper` store adapter would sit next to the EF one in `.Net/src/`. Same for `Omni2FA.MongoDB`, `Omni2FA.Grpc`, `Omni2FA.MinimalApi`, etc. The `Core/` backbone is unchanged.

> **Why split `.Net/Core/` and `.Net/src/`?** Same reason as the JS side: framework-agnostic code is held to a different review bar (no business-logic leaks, dependency surface kept minimal). Putting them in physically separate folders makes accidental ASP.NET-imports in `Omni2FA.Core` visible during code review at the path level, before reading a single line.

> **Why is JS-core in `Core/js/` but .NET-core in `.Net/Core/` instead of `Core/dotnet/`?** Asymmetry by design: `Core/js/` is shared between **multiple JS UI families** (React, Vue, Angular...) so it surfaces to the top. `.Net/Core/` is the backbone for a single backend family (ASP.NET Core, currently), so it nests inside that family's folder. This keeps `.Net/Omni2FA.sln` self-contained — open one solution file and the whole .NET tree is reachable in Solution Explorer.

---

## 7. Storage abstraction

Token persistence (pre-auth token survival across page reloads, "remember device" cookies) is a runtime concern, not a framework one. Core defines:

```ts
interface IStorage {
    get(key: string): string | null;
    set(key: string, value: string): void;
    remove(key: string): void;
}
```

Core ships three implementations:
- `MemoryStorage` — default, lost on reload. Safe for SSR.
- `LocalStorageStorage` — persistent across tabs and reloads. Browser only.
- `SessionStorageStorage` — persistent within a tab. Browser only.

Framework packages **don't pick a storage** — they accept whichever the host passes to `<Omni2FaProvider storage={...} />`. Default is `MemoryStorage` if not specified.

On the .NET side the equivalent abstraction is `IPreAuthTokenSink` (where to issue / how to validate the JWT). Default = stateless JWT signed by app key. Pluggable to redis/database if the host needs revocation.

**Pre-auth token transport** is Bearer-only by design through the v1.x line. An opt-in cookie transport (`Set-Cookie: HttpOnly; Secure; SameSite`) lands in v1.6 as a parallel option, not a replacement — OpenAPI will then declare both `bearerAuth` and `cookieAuth` security schemes and hosts choose via `o.PreAuth.Transport`. Bearer stays the default because it requires zero host configuration, works identically across every backend adapter (.NET, Node, Python, …), and keeps the OpenAPI contract self-contained (cookie-specific policies like `SameSite`, `Domain`, CSRF rotation are host concerns that don't belong in the contract). Cookie transport is recommended once the host's main session is also cookie-based and CSRF defenses are already in place.

**Request decoration is layered, not all-or-nothing.** The client owns one header (`Authorization`, routed per endpoint) and yields it to whatever the host set. Everything the host wants on top has a slot sized to it: `headers` (static or resolved per request) for header decoration, `credentials` for cookie policy, and `fetch` as the escape hatch for genuine transport concerns — retry, logging, a non-browser implementation. `fetch` must stay the *last* resort: it is handed a ready-made `Request`, so a wrapper that rebuilds it from `(url, init)` silently loses the headers, body, and credentials the client already applied. Any new "I need X on every request" need gets its own declarative config field rather than pushing hosts into the transport slot.

---

## 8. Configurability (binding)

Omni2FA targets **production apps that may already have their own custom 2FA implementation** they want to replace. Greenfield is welcome, but migration is a first-class scenario. The library must not lock users into Omni2FA-shaped defaults when they have existing data or naming conventions.

Anything that could collide with a host's existing state must be configurable via `Omni2FaOptions` — no hardcoded magic strings in core code.

**Required configuration points** (from v0.1):

| Setting | Default | Why configurable |
|---------|---------|------------------|
| `DataProtection.Scope` | `"Omni2FA"` | Hosts with existing TOTP secrets encrypted under a different scope (e.g. `"TwoFactorSecrets"`) must be able to point Omni2FA at their existing scope so secrets decrypt correctly. Without this, every user has to re-enroll. |
| `Issuer` (TOTP otpauth URI) | application name | Shown in authenticator apps next to the account. Must match what users see today. |
| Table / column names (EF mapping) | `Omni2FaMethods`, `Omni2FaChallenges` | Host migrating from `UserTwoFactorMethods` may want to keep the old table name to avoid renaming in queries / reports. Configurable via `modelBuilder.ApplyOmni2FaConfiguration(o => o.MethodsTableName = "UserTwoFactorMethods")`. |
| Pre-auth token TTL | 5 minutes | Different products have different UX tolerance. |
| TOTP tolerance window | ±1 step (±30 s) | Loose vs tight clock-skew tolerance. |
| Per-user WebAuthn credential cap | 3 | Hosts may want 5 or 10. |
| Verify rate limit | 20/min/IP | Configurable per host policy. |
| Allow disabling last method | true | Stricter hosts require 2FA — set false to forbid removing the last method. |

**Required pluggable services** (interfaces in `Omni2FA.Core`):

- `IOmni2FaAuditSink` — forward audit events into host's audit log.
- `IEmailSender` — replace default SMTP sender with host's email infrastructure.
- `ITwoFactorMethodStore` / `ITwoFactorChallengeStore` — replace EF store with custom backend (Mongo, Dapper, raw ADO).
- `IDataProtectionProvider` — pass host's existing provider (standard ASP.NET Core interface, not Omni2FA-specific).

**The test for "is this configurable enough?"** is migrating an existing custom 2FA implementation onto Omni2FA. Any host already running a hand-rolled 2FA should be able to plug Omni2FA in without modifying its database schema or rewriting business code — only adjusting `Omni2FaOptions` and EF mapping. Anything that requires touching Omni2FA source code instead of just config — that's a missing option.

## 9. Step-up (action confirmation)

Step-up re-confirms 2FA immediately before a sensitive action (change password, view recovery codes, remove a method), independent of the login flow. It follows the same core/adapter split as everything else.

**Where the logic lives**

- `Omni2FA.Core` owns the decision. `IStepUpEvaluator` takes the authenticated user id plus the presented token and returns a verdict — `Satisfied` (valid, single-use token consumed → allow), `NotEnrolledBypass` (user has no active method → allow), or `Required` (block). Token mint/validate is `IPreAuthTokenIssuer.IssueStepUp` / `ValidateStepUp`, carrying purpose `2fa-stepup` — distinct from the login `2fa-pending` / `2fa-verified`, so the three token kinds can never substitute for one another. The same `ITwoFactorChallengeService` verifies the code: `VerifyStepUpAsync` reuses the login verification core and only swaps which token it mints.
- `Omni2FA.AspNetCore` holds the thin adapters. `[RequireTwoFactor]` (MVC action filter) and `.RequireStepUp()` (minimal-API endpoint filter) both just read the header + current user and delegate to the evaluator; on `Required` they emit `403 STEP_UP_REQUIRED` with the available methods. The `/stepup/start|resend|verify` endpoints are session-authenticated mirrors of `/challenge/*`.

**Stateless transport + single use.** The proof is a short-lived signed JWT carried in `X-Omni2FA-StepUp` — no server lookup to validate it. It is bound to the user (the evaluator rejects a token whose subject ≠ the caller, so a stolen token cannot be replayed against another account) and consumed exactly once via `IStepUpNonceStore`, which records spent token ids until their expiry. The default `InMemoryStepUpNonceStore` is **single-instance**: across multiple nodes a token spent on one is not seen by the others, leaving a replay window bounded by the token TTL. Hosts running more than one instance register a shared `IStepUpNonceStore` (Redis/DB) to close it — the same pluggability pattern as the stores.

**Not weakenable by design.** There is no option to bypass an enrolled user; the only pass-through is "no method enrolled", which is intrinsic to the feature. The configurable surface is `StepUp.Ttl`, the header name, and the per-action protection flags below.

**The library's own destructive endpoints.** Host endpoints are protected by the host (attribute / `.RequireStepUp()`). But the endpoints `MapOmni2Fa` mounts — `DELETE /methods/{id}`, `/recovery-codes/regenerate`, `/enroll/*/start` — can't be decorated from outside, so they're gated by opt-in flags (`StepUp.RequireTwoFactorTo{Enroll,RemoveMethod,RegenerateRecoveryCodes}`, all default off). Because these are the *client's own* calls, the retry lives in the client: `IOmni2FaClient.setStepUpHandler` lets the client confirm 2FA and replay the call itself, so `useMethods`/enrollment hooks need no change. This is consistent with the transport rule — the client mediates step-up only for Omni2FA's own endpoints, never the host's. Recovery codes are one-way hashed, so "view existing codes" does not and must not exist; the gated recovery action is *regenerate*.

---

## 10. Why this matters

A common failure mode for "universal" libraries: framework adapter v1 accidentally absorbs business logic ("just a quick if for Email"), and by the time someone tries to add Vue support, half the FSM lives in React hooks. Porting then means rewriting, not wrapping.

Omni2FA explicitly chooses the harder path: write the FSM once, prove it works with two adapters (React-MUI in v0.5, React-Tailwind in v1.3) **before v1.0 is frozen**, and have a code review checklist that catches drift.

This document is the contract for that choice.

---

## 11. Change log

- **2026-05-20** — initial draft from session 1. Captures the framework-agnostic core / thin adapter principle as a binding rule, with boundary map, code review checklist, and dependency graph.
- **2026-05-20** — `.NET` physical layout updated to mirror the boundary: `Omni2FA.Core` and `Omni2FA.WebAuthn` moved from `.Net/src/` to `.Net/Core/`. `.Net/src/` now holds only ASP.NET-coupled adapters. `Omni2FA.sln` lives in `.Net/` root, references both folders. Rationale: makes the framework-agnostic boundary visible at the path level during code review.
- **2026-05-21** — added section 8 "Configurability (binding)". Migration from existing custom 2FA implementations is a first-class scenario; no hardcoded magic strings in core, all collision-prone settings exposed via `Omni2FaOptions` or pluggable interfaces. Original "Why this matters" renumbered to 9, change log to 10.
- **2026-05-21** — .NET dependency graph clarified: adapter packages (`Omni2FA.AspNetCore.EntityFrameworkCore`, future `Omni2FA.Dapper`, etc.) are **siblings**, all depending only on `Omni2FA.Core` plus their own infrastructure SDK. EF adapter no longer references `Omni2FA.AspNetCore`. Hosts can pull just the EF adapter without dragging in ASP.NET endpoints.
- **2026-05-21** — pre-auth token transport principle recorded in section 7: Bearer-only by design through v1.x, opt-in cookie transport scheduled for v1.6 as a parallel option (not a replacement). Rationale: Bearer requires zero host config, behaves identically across every backend adapter, and keeps the OpenAPI contract free of cookie-policy concerns that belong to the host's session strategy.
- **2026-06-08** — the standalone `Omni2FA.WebAuthn` package was folded into `Omni2FA.AspNetCore` (.NET drops from 4 to 3 packages). WebAuthn is a first-class 2FA method every ASP.NET host receives anyway; a separate package was speculative future-proofing (swappable crypto lib) that no consumer needed. The `IWebAuthnCeremonyService` contract stays in `Omni2FA.Core`, so the core remains FIDO2-free — only the Fido2NetLib implementation moved into the adapter. Section 6 updated to match.
- **2026-06-08** — added binding UI-customization principles to section 2 (styled packages): composable pieces (not a monolith), slot/`sx`/theme overrides on every part, no baked-in copy, and the three-tier model (hooks → headless-structural → styled skins). Styled packages must be adaptable to the host's ecosystem, not take-it-or-leave-it.
- **2026-06-10** — added section 9 "Step-up (action confirmation)" for the v0.7.3 step-up feature: core `IStepUpEvaluator` and the `2fa-stepup` token purpose, ASP.NET `[RequireTwoFactor]` / `.RequireStepUp()` filters and `/stepup/*` endpoints, and single-use enforcement via `IStepUpNonceStore` (in-memory default, pluggable for multi-instance). "Why this matters" renumbered to 10, change log to 11.
- **2026-07-27** — v0.9.0: recorded the layered request-decoration rule in section 7. `fetch` had become the only extension point for "add a header to every request", which pushes hosts into rebuilding the `Request` openapi-fetch hands them — and silently dropping the headers/body the client set. Header decoration now has its own declarative slot (`Omni2FaClientConfig.headers`, static or resolved per request); the transport slot is reserved for transport.
- **2026-06-10** — v0.8.0: extended section 9 with protection of the library's own destructive endpoints — opt-in `StepUp.RequireTwoFactorTo*` flags gating `/enroll/*/start`, `DELETE /methods/{id}`, `/recovery-codes/regenerate`, plus client-side confirm-and-retry via `IOmni2FaClient.setStepUpHandler` (so hooks/enrollment machines are untouched). Recorded that recovery "view" cannot exist (one-way hashed) — the gated action is regenerate.
