# Omni2FA

> Drop-in **multi-method two-factor authentication** for self-hosted apps — TOTP, Email OTP, WebAuthn/passkeys, recovery codes — behind one OpenAPI contract.

[![npm](https://img.shields.io/npm/v/%40omni2fa%2Fcore?logo=npm&label=%40omni2fa%2Fcore&color=cb3837)](https://www.npmjs.com/package/@omni2fa/core)
[![NuGet](https://img.shields.io/nuget/v/Omni2FA.AspNetCore?logo=nuget&label=Omni2FA.AspNetCore&color=004880)](https://www.nuget.org/packages/Omni2FA.AspNetCore)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-pre--1.0-orange.svg)](#status)

You verify the password and mint your session; Omni2FA handles everything 2FA in between — enrollment, the login challenge, OTP/WebAuthn ceremonies, recovery codes, persistence, rate limiting, audit. **Backend service + endpoints + EF store on .NET; headless hooks on React.** Other stacks implement the same contract (roadmap below).

For apps that **own their user table** (ASP.NET Core Identity, custom JWT/cookie auth, …). Not for managed identity providers (Auth0, Clerk, Cognito, Firebase, Supabase, Okta, WorkOS) — they already ship 2FA.

---

## Packages

| Stack | Install | Notes |
|-------|---------|-------|
| **.NET** (ASP.NET Core) | `Omni2FA.AspNetCore` + `Omni2FA.AspNetCore.EntityFrameworkCore` | `Omni2FA.Core` comes transitively |
| **React** | `@omni2fa/core` + `@omni2fa/react` | hooks; styled MUI dialogs (`@omni2fa/react-mui`) — planned |

Other backends (Node, Python) and frontends (Angular, Vue) are on the [roadmap](docs/ROADMAP.md); any stack can implement the [OpenAPI contract](Core/protocol/omni2fa.openapi.yaml).

---

## Backend — ASP.NET Core

```bash
dotnet add package Omni2FA.AspNetCore
dotnet add package Omni2FA.AspNetCore.EntityFrameworkCore
```

**`Program.cs`**
```csharp
builder.Services.AddOmni2Fa(o => builder.Configuration.GetSection("Omni2Fa").Bind(o));
builder.Services.AddOmni2FaEntityFrameworkStore<AppDbContext>();   // or implement the store interfaces yourself

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapOmni2Fa();                                                  // mounts /api/2fa/*
```

**Your `DbContext`**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyOmni2FaConfiguration();                      // 2FA tables (names configurable)
}
```

Hook the **pre-auth token** into your existing login — the seam between your password check and the 2FA step:

```csharp
public class AuthService(IPreAuthTokenIssuer preAuth, ITwoFactorMethodStore methods) {
    // after you verify the password:
    if (await methods.HasActiveMethodsAsync(userId)) {
        var ticket = preAuth.Issue(userId);                        // short-lived JWT
        var available = await methods.ListActiveByUserAsync(userId);
        return Challenge(ticket.Token, ticket.ExpiresAt, available); // your DTO → frontend
    }
    // else: mint your session as usual

    // when the frontend finishes 2FA, it calls your "finalize" with the verifiedToken from the verify response:
    var verifiedUserId = preAuth.ValidateVerified(verifiedToken);  // null if not actually verified → reject
}
```

**Use the DTOs the library already ships** at the host seam — don't reinvent them (all in `Omni2FA.Core.Dtos` / `.Services`):

| DTO | Where you use it |
|-----|------------------|
| `PreAuthTokenInfo` | returned by `Issue` / `IssueVerified` — `{ Token, ExpiresAt }` |
| `PreAuthChallengeResponse` | your login response when 2FA is required — `{ PreAuthToken, AvailableMethods, ExpiresAt }` |
| `TwoFactorMethodDto` | shape of an enrolled method (`AvailableMethods` items) |
| `VerifySuccessResponse` | the `/challenge/verify` body — `{ UserId, VerifiedToken, ExpiresAt }` |
| `ErrorResponse` + `Omni2FaErrorCodes` | error envelope and the stable error-code constants |

**`appsettings.json`**
```jsonc
"Omni2Fa": {
  "PreAuth":  { "SigningKey": "<32+ char HMAC key — from env/secrets>" },
  "Totp":     { "Issuer": "MyApp" },
  "Email":    { "FromAddress": "no-reply@myapp.com",
                "Smtp": { "Host": "smtp.example.com", "Port": 587, "Username": "...", "Password": "...", "UseStartTls": true } },
  "WebAuthn": { "RelyingPartyId": "myapp.com", "Origins": [ "https://myapp.com" ] },
  "StepUp":   { "RequireTwoFactorToEnroll": true, "RequireTwoFactorToRemoveMethod": true, "RequireTwoFactorToRegenerateRecoveryCodes": true }  // opt-in (default false): step-up on Omni2FA's own destructive endpoints
}
```

That's the whole backend: endpoints, the three methods, recovery codes, rate limiting and audit are live.

---

## Frontend — React

```bash
npm i @omni2fa/core @omni2fa/react
```

**Once, at the app root**
```tsx
import { createOmni2Fa } from '@omni2fa/core';
import { Omni2FaProvider } from '@omni2fa/react';

export const omni = createOmni2Fa({ baseUrl: '/api/2fa' });
// after your login, hand the client your host session token (or use cookies: createOmni2Fa({ credentials: 'include' })):
omni.client.setSessionToken(sessionToken);

<Omni2FaProvider value={omni}>{children}</Omni2FaProvider>
```

**Sending your own headers** — a routing flag, the UI language, an active tenant. Use `headers`, not a custom `fetch`; a function is re-evaluated on every request, so runtime changes (language switch, tenant switch) are picked up:
```ts
createOmni2Fa({
  baseUrl: '/api/2fa',
  credentials: 'include',
  headers: { 'X-Portal-Auth': '1' },                       // static
});

createOmni2Fa({
  baseUrl: '/api/2fa',
  headers: () => ({ 'Accept-Language': i18n.language }),   // resolved per request
});
```
The `fetch` option stays the escape hatch for real transport concerns (retry, logging). It is called with a ready-made `Request` — forward it as-is (`(input, init) => fetch(input, init)`); rebuilding the request from `init` drops the headers and body the client already set.

The hooks expose state + actions; you render the UI (headless).

**Login challenge**
```tsx
import { useChallenge } from '@omni2fa/react';

const { status, context, pick, submit, useRecoveryCode } = useChallenge();
// pick(methodId) → submit(code)  (WebAuthn auto-runs the browser ceremony)
// status: 'idle' | 'awaitingCode' | 'asserting' | 'verifying' | 'verified' | 'failed' | …
// on 'verified': send context.verifiedToken to your finalize endpoint (not the pre-auth token)
```

Hooks: `useMethods`, `useTotpEnrollment`, `useEmailEnrollment`, `useWebAuthnEnrollment`, `useChallenge` (+ `*Selector` variants). A full headless UI you can copy lives in [`examples/full/frontend`](examples/full/frontend). Styled drop-in components (`@omni2fa/react-mui`) are planned.

---

## Step-up — confirm sensitive actions

Force a fresh 2FA check right before a sensitive action (change password, view recovery codes, remove a method). Decorate the endpoint: if the user has 2FA enrolled they must confirm it; if they don't, the call passes through. A stolen session alone can't perform the action.

**Backend** — one attribute on an MVC action, or `.RequireStepUp()` on a minimal-API endpoint:
```csharp
[HttpPost("change-password")]
[RequireTwoFactor]                         // → 403 STEP_UP_REQUIRED until a valid step-up token is sent
public Task<IActionResult> ChangePassword(ChangePasswordRequest req) { … }

// minimal API:
app.MapPost("/account/email", ChangeEmail).RequireAuthorization().RequireStepUp();
```

**Frontend** — `useStepUp()` gives you `confirmTwoFactor(methods)`: it shows the 2FA prompt and resolves a single-use token (or `null` if cancelled). You attach that token in the `X-Omni2FA-StepUp` header on your request — over fetch or axios, cookie or Bearer session; the library never touches your transport. Two ways to use it:

**Reactive** — let the server tell you. Best in one central place (an axios/fetch interceptor, right next to your `401` handling) — covers every protected endpoint at once:
```tsx
import { STEP_UP_HEADER, Omni2FaErrorCodes } from '@omni2fa/core';

// in your response interceptor, when a call returns 403:
const err = await res.clone().json();
if (err.code === Omni2FaErrorCodes.StepUpRequired) {
  const token = await confirmTwoFactor(err.details.availableMethods);
  if (token) res = await replayRequest({ [STEP_UP_HEADER]: token });   // retry with the header
}
```

**Proactive** — when you already know an action needs 2FA, confirm up-front and skip the 403 round-trip. You supply the methods yourself (e.g. from `useMethods()`):
```tsx
const { confirmTwoFactor } = useStepUp();
const { items: methods } = useMethods();

async function changePassword() {
  let headers = {};
  if (methods.length > 0) {                       // no 2FA enrolled → nothing to confirm, server lets it through
    const token = await confirmTwoFactor(methods);
    if (!token) return;                           // cancelled
    headers = { [STEP_UP_HEADER]: token };
  }
  await api.changePassword(body, headers);        // sent already carrying the token
}
```

While a prompt is `active`, render the 2FA UI (reuse your challenge UI): `methods` → `pick(id)` → `submit(code)`.

**Protecting the library's own endpoints** — remove method, regenerate recovery codes, and enroll a new factor are mounted by `MapOmni2Fa`, so you can't decorate them. Turn them on with the per-action `StepUp.RequireTwoFactorTo*` flags (in `appsettings.json` above; all off by default — and recovery codes can't be *viewed*, only regenerated, so that's the gated action). The frontend needs no wiring: mounting `useStepUp()` anywhere in the tree registers its prompt on the client for you.
```tsx
const { confirmTwoFactor /* + prompt state to render */ } = useStepUp();
// registered automatically while mounted; opt out with useStepUp({ handleClientStepUp: false })
// when you register your own handler via omni.client.setStepUpHandler(...)
```
Now `omni.client.removeMethod(...)` / `regenerateRecoveryCodes()` / enrollment prompt for 2FA and retry automatically. A user with no method enrolled is never blocked.

The step-up token is **single-use** — one confirmed 2FA per protected action. Consumed token ids are kept **in memory by default**, so on a multi-instance deployment a token spent on one node isn't known to the others — a brief replay window within the token TTL. Register a shared `IStepUpNonceStore` (e.g. Redis) to close it. Details in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

**Asked too often?** A user managing their 2FA hits three prompts in a row, and someone who just signed in with 2FA gets asked again the moment they open a protected page. One **grace window** fixes both, off by default:

```jsonc
"StepUp": { "GraceWindow": "00:01:00" }
```

A passed 2FA challenge counts for that long, whether it was a step-up confirmation or the login itself — both prove the same thing, so there is one setting, not two.

Nothing to configure on the frontend — the server returns the window with each confirmation and the client honours it.

A **recovery-code login never** gets a window: that is the flow of someone who lost their factors, and equally of someone who took the account over. The window rides inside the token, so only the browser that confirmed skips the prompt — another session of the same user still confirms, which is the point: a per-user "confirmed recently" flag would hand the free pass to a stolen session too. Keep the window in minutes; `StepUp.Ttl` is uncapped, so setting everything to hours turns the barrier into a formality. This is **not** "remember this browser" — that's roadmap v1.3.

---

## Two logins in one app (staff + customers)

Applications that sign in more than one population — staff at `/api/auth`, customers at `/api/portal/auth`, separate tables, ids that overlap — declare each as an **audience**: its own mount, its own subject namespace, its own auth scheme.

```csharp
services.AddOmni2Fa(o => {
    o.AspNetCore.Audiences.Add(new Omni2FaAudienceOptions {
        Name = "customer",
        RoutePrefix = "/api/portal/2fa",     // put it under the path that already means "customer"
        SubjectPrefix = "customer:",         // customer 42 and staff 42 are different users
        AuthenticationSchemes = "CustomerCookie",
    });
});

app.MapOmni2Fa();                            // staff  → /api/2fa
app.MapOmni2Fa("customer");                  // portal → /api/portal/2fa

app.MapGroup("/api/portal").WithOmni2FaAudience("customer");   // or [Omni2FaAudience("customer")] on a controller
```

Mounting under the portal's own path means whatever already tells your backend "this is a customer request" — the cookie you read, the scheme you pick, the CORS policy — covers the 2FA endpoints too, with nothing for the frontend to flag. Tag your own `[RequireTwoFactor]` endpoints with the audience as well, so step-up is evaluated against the right subject and points the caller at that audience's `/stepup`.

The namespace is applied for you on every mounted endpoint. Where your code talks to Omni2FA directly — issuing the pre-auth token at login, reading the subject out of a verified-handoff token at finalize — take the subject from the registry instead of concatenating it yourself:

```csharp
var subject = audiences.ToSubject("customer", customer.Id.ToString());   // IOmni2FaAudienceRegistry
if (!audiences.TryGetUserId("customer", verifiedSubject, out var id)) return Unauthorized();
```

On the frontend each audience is a separate client — its own `baseUrl`, and a `namespace` so two logins in one browser never share a storage key:

```ts
export const omni = createOmni2Fa({ baseUrl: '/api/2fa', credentials: 'include' });
export const portalOmni = createOmni2Fa({ baseUrl: '/api/portal/2fa', credentials: 'include', namespace: 'portal' });
```

Single-login apps ignore all of this: the default audience is implicit and nothing changes. Design notes in [`docs/ASPNETCORE.md`](docs/ASPNETCORE.md) §7.

---

## Configuration (key options, under `Omni2Fa`)

| Option | Default | Purpose |
|--------|---------|---------|
| `PreAuth.SigningKey` | — (required, ≥32 chars) | HMAC key for the pre-auth ticket; validated at startup |
| `PreAuth.Ttl` | 5 min | Pre-auth ticket lifetime |
| `PreAuth.VerifiedTtl` | 2 min | Verified-handoff token lifetime (the finalize proof) |
| `StepUp.Ttl` | 5 min | Step-up token lifetime — gap allowed between confirming 2FA and the action |
| `StepUp.GraceWindow` | `0` (off) | How long a passed 2FA challenge (step-up **or** login) covers further protected actions; must be ≤ `StepUp.Ttl`. Never granted for a recovery-code login |
| `StepUp.RequireTwoFactorTo{Enroll,RemoveMethod,RegenerateRecoveryCodes}` | `false` | Gate the library's own destructive endpoints with step-up (opt-in, per action) |
| `Totp.Issuer` | `Omni2FA` | Name shown in authenticator apps |
| `Email.Smtp.*` / `Email.BackgroundDelivery` | — / `true` | SMTP transport; codes sent on a background worker by default |
| `WebAuthn.RelyingPartyId` / `Origins` | `localhost` / `http://localhost:5173` | Must match your real hostname (HTTPS off-localhost) |
| `WebAuthn.MaxCredentialsPerUser` | 3 | Passkey cap per user |
| `RateLimit.{PermitLimit,Window}` | 20 / 1 min | Per-IP limit on verify/enroll endpoints |
| `AspNetCore.AllowDisablingLastMethod` | `true` | Set `false` to forbid removing the last method |
| `AspNetCore.Audiences` | empty | Separately authenticating populations (staff + customers): mount, subject namespace and auth scheme per audience |
| EF table/column names | `Omni2Fa*` | Override via `ApplyOmni2FaConfiguration(o => …)` for migrations |

---

## Extension points (swap any piece — `TryAdd`, host wins)

| Interface | Replace to… |
|-----------|-------------|
| `IEmailSender` | send via your own infra (SendGrid/SES/relay) instead of MailKit/SMTP |
| `IEmailMessageBuilder` | customize/localize the OTP email copy |
| `IOmni2FaAuditSink` | forward audit events to your log/SIEM (default → `ILogger`) |
| `ITwoFactorMethodStore` / `ITwoFactorChallengeStore` / `IRecoveryCodeStore` | use Mongo/Dapper/raw ADO instead of EF Core |
| `IUserContextAccessor` | derive the current user id from a custom claim/header |
| `IStepUpNonceStore` | share single-use step-up token ids across instances (Redis/DB) — default is in-memory |
| `IPreAuthTokenIssuer` | change how the pre-auth ticket is minted/validated |
| `IWebAuthnCeremonyService` | swap the FIDO2 implementation |

---

## Methods & status

| Method | State |
|--------|-------|
| TOTP (authenticator apps) | ✅ |
| Email OTP (SMTP) | ✅ |
| WebAuthn (passkeys / security keys) | ✅ |
| Recovery codes (one-time, hashed) | ✅ |
| Rate limiting · audit sink | ✅ |

🚧 **Status — pre-1.0 (0.6.x).** Functionally complete for .NET + React, verified by a live end-to-end run, but young: no automated test suite yet (planned for v1.0), and WebAuthn/email not yet battle-tested across many environments. Suitable for your own apps; harden before betting a production product on it.

---

## More

- [`examples/full`](examples/full) — runnable ASP.NET + React + SQLite app with all methods.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) · [`docs/FLOWS.md`](docs/FLOWS.md) · [`docs/ROADMAP.md`](docs/ROADMAP.md) · [`docs/PUBLISHING.md`](docs/PUBLISHING.md)
- [`Core/protocol/omni2fa.openapi.yaml`](Core/protocol/omni2fa.openapi.yaml) — the cross-stack contract.

## License

MIT — see [LICENSE](LICENSE).
