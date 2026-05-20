# Omni2FA

> Drop-in **multi-method two-factor authentication** for any stack.
> One contract — pick your frontend, pick your backend, plug it in.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-WIP-orange.svg)](#status)

Omni2FA bundles **TOTP** (authenticator apps), **Email OTP**, and **WebAuthn** (passkeys, security keys) into a single coherent flow — with ready UI and ready endpoints. You don't write enrollment dialogs, OTP storage, or login challenge plumbing.

---

## Who this is for

**Self-hosted apps that own their user database** — and want a richer 2FA story than what their auth framework provides out of the box.

✅ **Great fits**

- **ASP.NET Core Identity** apps. Identity ships TOTP and that's it (single secret per user, no email OTP, no WebAuthn, no UI). Omni2FA layers on top — plug into your `UserManager`, keep your login flow, gain multi-method 2FA.
- **Custom JWT / cookie auth** rolled by hand or with a small framework — same story, you own the user table, we handle 2FA.
- **Django, Rails, Express, FastAPI** apps where you control the user model. Backend adapter for non-.NET stacks lands in v2+; the React/Angular frontend will work against any backend that implements the OpenAPI contract once it's frozen at v1.0.

❌ **Not for you if**

- You use a **managed cloud identity provider** — **Auth0, Clerk, Cognito, Firebase Auth, Supabase Auth, Okta, WorkOS**. Your provider already ships 2FA in its dashboard. Use theirs.

> ASP.NET **Core Identity** is *not* a managed provider — it's a library inside your app. It's a great fit. The "Auth0/etc." exclusion is about *cloud-hosted* identity, where you don't own the user record.

---

## Why this exists

> I built 2FA from scratch two times across two different products. Omni2FA exists so I — and you — never have to do it a third time.

Existing libraries do *pieces*: TOTP math, WebAuthn ceremony, OTP generation. Stitching them into a real product — multi-method per user, login orchestration, enrollment UX, email delivery, persistence — is on you every time.

Omni2FA gives you the **whole loop** as a library:

- Backend service + endpoints + persistence adapter.
- Frontend hooks + ready dialogs.
- A shared HTTP contract so any frontend works with any backend.

---

## Goals

- ⚡ **Minutes to integrate.** Add the packages, configure SMTP and a store, render one section in your profile page — done.
- 🔌 **Mix and match stacks.** React frontend with .NET backend today. Add Python or Angular tomorrow — the contract stays the same.
- 🧱 **Persistence is yours.** We define the storage interface and ship an optional EF Core adapter. You can swap in Mongo, Dapper, or anything else.
- 🧩 **Customize the surface, not the core.** Forms, themes, copy, callbacks — open. Crypto, challenge state machine, validation — closed.
- 🔒 **Standard primitives only.** Built on `OtpNet`, `Fido2NetLib`, `MailKit`, `@simplewebauthn/*`. No hand-rolled crypto.

---

## Repository layout

```
Omni2FA/
├── Core/
│   ├── protocol/    # OpenAPI spec + JSON schemas — the cross-stack contract
│   └── js/          # @omni2fa/core — shared TypeScript logic for any JS frontend
│
├── React/
│   ├── react/       # @omni2fa/react — headless hooks + base components
│   └── react-mui/   # @omni2fa/react-mui — ready dialogs styled with MUI
│
├── .Net/                              # Self-contained .NET solution
│   ├── Omni2FA.sln
│   ├── Core/                          # Framework-agnostic .NET backbone
│   │   ├── Omni2FA.Core/              # Models, interfaces, services (no I/O, no ASP.NET, no EF)
│   │   └── Omni2FA.WebAuthn/          # WebAuthn ceremony (Fido2NetLib only)
│   └── src/                           # ASP.NET-specific adapters
│       ├── Omni2FA.AspNetCore/                    # Endpoints, DI, filters, email
│       └── Omni2FA.AspNetCore.EntityFrameworkCore/ # Optional EF Core store adapter
│
├── examples/        # End-to-end demo apps
└── docs/            # Architecture notes, ADRs, API guides
```

---

## How it will look (target API)

> ⚠️ Code below is the **target shape** — packages are not published yet. See [Status](#status).

### Backend — ASP.NET Core

```csharp
// Program.cs
builder.Services.AddOmni2Fa(o => {
    o.Issuer = "MyApp";
    o.Smtp.Host = "smtp.example.com";
    o.Smtp.Port = 587;
    o.Smtp.Username = builder.Configuration["Smtp:User"];
    o.Smtp.Password = builder.Configuration["Smtp:Pass"];
});

builder.Services.AddOmni2FaEntityFrameworkStore<AppDbContext>();

var app = builder.Build();
app.MapOmni2FaEndpoints();   // mounts /api/2fa/* on your app
```

Hook the 2FA tables into your existing `DbContext`:

```csharp
public class AppDbContext : DbContext {
    public DbSet<User> Users { get; set; }
    // ... your entities

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOmni2FaConfiguration();
    }
}
```

That's the backend side. SMTP is configured, store is plugged in, endpoints are live.

### Frontend — React

```tsx
import { TwoFactorSection } from '@omni2fa/react-mui';

export default function ProfilePage() {
    return (
        <ProfileLayout>
            <ProfileFields />
            <TwoFactorSection apiBaseUrl="/api/2fa" />
        </ProfileLayout>
    );
}
```

That's it. The component renders the method list, enrollment dialogs, removal flow, and login-step UI. Want to roll your own visuals? Use `@omni2fa/react` instead and compose with the headless hooks (`useEnrollTotp`, `useLoginChallenge`, etc.).

### Mix and match

Same React component works against a Python backend that implements the Omni2FA OpenAPI contract. Same .NET backend works with an Angular frontend that does. The contract is the integration point — not the language.

---

## Methods supported

| Method            | Status      | Notes                                                       |
|-------------------|-------------|-------------------------------------------------------------|
| TOTP              | Planned v0.1 | Authenticator apps — Google Authenticator, Authy, 1Password |
| Email OTP         | Planned v0.2 | Server-issued 6-digit code, sent via configured SMTP        |
| WebAuthn          | Planned v0.3 | Passkeys & hardware keys — multiple credentials per user    |
| Recovery codes    | Planned v0.4 | One-time backup codes, generated on first method enrollment |
| Trusted devices   | Planned v1.1 | "Remember this browser" — skip 2FA on known devices         |
| SMS               | v2+ (demand-driven) | Carrier cost & complexity — opt-in pluggable sender |

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the full version plan, [`docs/FLOWS.md`](docs/FLOWS.md) for login/enrollment/recovery flow diagrams, [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the framework-agnostic core / thin adapter contract, and [`Core/protocol/`](Core/protocol/) for the OpenAPI 3.1 contract that defines every endpoint and DTO across all stacks.

---

## Built-in by design

- 🔁 **Multi-method per user.** Mix and match — one user can have TOTP, email, and three WebAuthn credentials at once.
- 🎟️ **Pre-auth token** (industry-standard MFA ticket) between password and 2FA verify — frontend never has to carry user identity in the URL.
- 🧨 **Recovery codes** generated on first method enrollment, shown once, hashed at rest.
- ⚡ **Rate limiting** out of the box — default 20 attempts/minute/IP on verify endpoints. Configurable, with a sensible-by-default brute-force ceiling that won't annoy real users.
- 📜 **Audit sink** — optional `IOmni2FaAuditSink` interface for enrollment, verify, and recovery events. Plug into your existing audit pipeline, or skip it and we just log to `ILogger`.
- 🌍 **i18n-ready** — email templates and UI strings translate via standard mechanisms (`IStringLocalizer<T>` on .NET, `react-i18next` on the React side).

---

## Explicitly out of scope

These are **not** going into Omni2FA, even later. They belong to the host application:

- **Account recovery when a user loses everything** (methods + recovery codes). This is a business decision — support ticket, admin override, trusted contact, identity proofing. Omni2FA only provides the primitive (admin can call "reset all 2FA for user X" via the API), the *policy* around it is yours.
- **Password authentication itself.** Omni2FA layers on top of your existing login. You verify the password; we handle everything after.
- **Session management / JWT issuance.** We return "verified, here is the user id" — your app mints its session.
- **User management UI.** Profile/settings pages are yours; we only provide the 2FA section.

---

## Status

🚧 **Pre-alpha — under active design.** No packages published yet. The repository is currently being scaffolded; the API examples above describe the target shape and are subject to change before v0.1.

Track progress in [`docs/`](docs/) (architecture notes will land there as decisions are made).

---

## License

MIT — see [LICENSE](LICENSE).
