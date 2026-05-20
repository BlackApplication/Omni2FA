# Omni2FA — Code Style & Conventions

This is a **living document**. Whenever a new rule is agreed upon during a session, it lands here so the next session picks it up automatically. Keep entries short. Rationale only when non-obvious.

---

## 1. File organization

- **One concept per file.** One class, one interface, one enum, one type alias, one DTO, one constant group, one helper module — each in its own file.
- **Folders by purpose**, not by feature size. Typical buckets:
  - `Constants/`
  - `Enums/`
  - `Models/` or `Entities/` (domain POCO)
  - `Dtos/` (transport DTOs)
  - `Interfaces/`
  - `Services/`
  - `Helpers/` or `Utils/`
  - `Extensions/`
  - `Configurations/` (.NET EF) or `config/` (JS)
- Never dump multiple unrelated types into a single file because they happen to be small.

## 2. Braces & line wrapping (C# and TypeScript)

- **K&R brace style everywhere.** Opening `{` stays on the same line as the method/class/`if`/`for`/`while`/`switch` signature. This **overrides** the Visual Studio default for C# — `.editorconfig` enforces it.
  ```csharp
  public class TotpService : ITotpService {
      public string GenerateSecret() {
          // ...
      }
  }
  ```
- **Always use braces** for `if`/`else`/loops, even one-line bodies.
- **Parameter wrapping:** methods/constructors/calls with **3 or fewer** parameters stay on one line. **4+** parameters → wrap one per line. But before wrapping, ask: can the signature be collapsed into a single options DTO? If yes — prefer the DTO.
- **Line length tolerance:** ~190 characters is fine before considering wrapping. Wide monitors are standard; more code on screen beats premature line splits.
- **Generic constraints (`where T : ...`)** stay on the same line as the method signature when they fit:
  ```csharp
  public static IServiceCollection AddOmni2FaEntityFrameworkStore<TDbContext>(this IServiceCollection services) where TDbContext : DbContext {
      ...
  }
  ```
- **Fluent chains** (LINQ, builder calls) wrap per call — that's data-flow readability, not a signature break.
- **Boolean condition wrapping:** 3 or fewer `&&`/`||` clauses stay on one line. **4+** clauses → wrap one per line.

## 3. Naming

- C# — PascalCase for types/methods/properties, camelCase for parameters/locals, `_camelCase` for private fields.
- TypeScript — PascalCase for types/components, camelCase for functions/variables, SCREAMING_SNAKE_CASE for module-level constants.
- File names match the primary export: `TotpService.cs`, `useEnrollTotp.ts`, `TwoFactorMethodDto.cs`.
- No abbreviations that aren't industry-standard (`TOTP`, `OTP`, `JWT` ok; `2FA` ok; `usrSvc` not ok).

## 4. TypeScript & React

- **Latest stable React** (currently 19.x) and **latest stable TypeScript** (currently 5.x). Track major versions actively — don't lag.
- **TypeScript is mandatory for all React code.** No plain JavaScript except generated build output.
- Prefer `null` over `undefined` for "no value" in interfaces, DTO fields, and state. This mirrors C# nullable semantics so cross-stack DTOs round-trip naturally.
  - `field: string | null` ✅
  - `field?: string` ❌ (means `field: string | undefined`)
- `interface` for object shapes that get extended; `type` for unions, primitives, intersections.
- Strict mode on (`strict: true` in tsconfig). No `any` unless it's a third-party gap with a comment.

## 5. C# / .NET

- Target framework: **net8.0** (current LTS, supported through Nov 2026). Migrate to net10.0 as soon as it ships (LTS, Nov 2026).
- `Nullable enable` globally. `ImplicitUsings enable`.
- DI through constructor injection. No service locators.
- No MediatR / CQRS — plain services with interfaces.
- Async methods end in `Async` and take a `CancellationToken`.
- Use `IOptions<T>` for configuration, never raw `IConfiguration` reads in services.

## 6. Reuse & helpers

- One source of truth — duplicated logic is a bug.
- If a component / function is used in two places → extract.
- When a method grows past ~40 lines or two responsibilities → split into helpers in the same folder.
- Helpers live in `Helpers/` (.NET) or `utils/` (JS) and are stateless functions.

## 7. DTO / model design

- **Keep models flat and simple.** No deep inheritance hierarchies, no abstract base classes that exist "in case".
- DTOs carry data, not behavior. No methods on DTOs.
- Long constructor signatures → introduce an options/request DTO (see rule 2).

## 8. Comments

- Default: no comments. Identifier names should explain *what*.
- A comment is justified only when *why* is non-obvious — a workaround, a hidden constraint, an invariant, units (UTC, bytes, seconds), format examples, non-obvious lifecycle. Keep it short.
- Never reference task tracking, "added for X flow", or commit metadata in code comments.

**Specific noise to avoid:**

- **`<inheritdoc />` alone on an implementation.** If the interface is documented, IntelliSense reads it through the interface. Add an XML doc on a concrete method only when there's something to add *beyond* what the interface says.
- **`<summary>` that just restates the identifier.** `Id` → `Identifier of the row` adds zero information. `Kind` → `The kind of factor` is the same. Delete those.

**Heuristic before keeping a comment:** if you removed it, would the reader lose anything the identifier doesn't already say? If no — delete.

## 9. Errors & results

- Cross-stack consumers (per OpenAPI contract) get **structured error codes**, not free-form messages. Error code list lives in `Core/protocol/`.
- Internally, services return either typed results or throw exceptions consistent with the layer:
  - Domain validation failure → typed error / result.
  - Programmer error / contract violation → exception.

## 10. Framework-agnostic core (binding)

This rule has its own document — see [`ARCHITECTURE.md`](ARCHITECTURE.md). Summary, in case you skim:

- `@omni2fa/core` and `Omni2FA.Core` are **framework-agnostic**. No `react`, no `vue`, no `@angular/core` imports in JS core. No `Microsoft.AspNetCore.*` references in .NET core.
- Framework / styled packages (`@omni2fa/react`, `@omni2fa/react-mui`, `Omni2FA.AspNetCore`, etc.) are **thin adapters**: they subscribe to core stores and render — no business logic, no timers, no `fetch`, no kind-specific branching, no validation.
- The adapter contract is `subscribe(cb) → unsub` + `getSnapshot(): TState`. Every framework wraps it in its own reactivity primitive (`useSyncExternalStore` / `customRef` / `toSignal`).
- Storage (where to keep pre-auth tokens) is abstracted behind `IStorage` in core. Framework packages don't pick a backend.
- Cross-package dependency graph is enforced in code review — see `ARCHITECTURE.md` §6.

If you're about to put a `setTimeout`, a `fetch`, an `if (kind === 'Totp')`, or a validation regex in a React/Vue/Angular file — **stop**, that belongs in core.

## 11. Tests (future)

- Tests live in `.Net/tests/` (mirrors `src/` layout) and per-package `__tests__/` in JS workspaces.
- No mocking of databases for integration tests — use an in-memory or container-backed real provider.

---

## Change log

- **2026-05-20** — initial draft from session 1. Captured rules (1)–(10) from Andrey's stated preferences.
- **2026-05-20** — clarified latest-stack stance: React 19+, TS 5+ (rule 4). .NET TFM left pending — .NET 9 reached EOL on 2026-05-12 and is not safe to ship; awaiting decision between net8.0 (current LTS, supported through Nov 2026) and waiting on net10.0 (next LTS, releases Nov 2026).
- **2026-05-20** — .NET TFM decided: **net8.0**. Plan: migrate to net10.0 at the earliest opportunity after its release (~Nov 2026).
- **2026-05-21** — clarified rule 2 ("Braces & line wrapping"): added ~190-char tolerance, explicit guidance for `where` generic constraints (same line as signature), and fluent chains (wrap per call is fine — data flow, not signature break).
- **2026-05-20** — additional architectural decisions captured (see `docs/FLOWS.md` & `docs/ROADMAP.md`):
  - Pre-auth token (industry term) replaces working name "challenge_token" everywhere — APIs, code, docs.
  - Recovery codes are first-class — v0.4, hashed at rest, generated on first method enrollment, shown once.
  - Default rate limit: **20 attempts / minute / IP** on verify endpoints (mirrors QRpark proven config). Configurable.
  - Audit is pluggable (`IOmni2FaAuditSink`), opt-in. Default = log to `ILogger`. No null refs if host doesn't register one.
  - Account recovery (lost methods + lost recovery codes) is out of scope — host application's policy. Omni2FA exposes a "reset all 2FA for user X" primitive only.
- **2026-05-20** — versioning model decided: **coordinated minor/major across all packages, independent patches** (Microsoft.AspNetCore-style). Packages on the same `MAJOR.MINOR.*` are guaranteed compatible — no compatibility matrix. See `docs/ROADMAP.md` "Versioning model" for examples. Earlier "independent per package" stance is retracted as it conflicted with milestone-based roadmap.
- **2026-05-20** — added rule 10 "Framework-agnostic core (binding)" pointing at the new `docs/ARCHITECTURE.md`. Tests rule renumbered to 11. The architecture document is binding: framework adapters are stateless subscribers, business logic lives in core only.
