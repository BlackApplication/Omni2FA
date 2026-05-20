# Omni2FA — Protocol (cross-stack contract)

This folder holds the **language-neutral source of truth** for Omni2FA's HTTP API. Every backend implementation (`Omni2FA.AspNetCore`, future Python adapter, …) and every frontend implementation (`@omni2fa/react`, future Vue/Angular, …) is generated from or verified against the spec in this folder.

If you change the contract, **the spec changes first**, code follows.

## Files

| File | Purpose |
|------|---------|
| `omni2fa.openapi.yaml` | OpenAPI 3.1 spec — endpoints, request/response shapes, security schemes, error envelope. |
| `ERROR_CODES.md` | Catalogue of error codes (the `code` field in `ErrorResponse`). Stable across versions. |
| `README.md` | This file. |

## Versioning

`info.version` in the OpenAPI doc matches the **Omni2FA library milestone** version. See `../../docs/ROADMAP.md` "Versioning model" — packages on the same `MAJOR.MINOR.*` are compatible and ship together as a coordinated release.

- `0.0.0` — pre-alpha (current). Spec may change without notice.
- `0.1.0` — first published milestone (TOTP). Spec is now versioned semver — contract-breaking changes bump minor during 0.x.
- `1.0.0` — frozen. Breaking changes bump major from here on.

## Tooling

### Generating TypeScript types

```bash
npx openapi-typescript Core/protocol/omni2fa.openapi.yaml -o Core/js/src/generated/openapi-types.ts
```

This produces a `paths` and `components` type tree that `@omni2fa/core`'s HTTP client consumes — no manual DTO duplication on the JS side.

### Generating .NET clients

```bash
dotnet tool install -g Microsoft.OpenApi.Kiota
kiota generate -l CSharp -d Core/protocol/omni2fa.openapi.yaml -o .Net/Core/Omni2FA.Core/Generated/
```

Used for client-side scenarios (consumers calling Omni2FA-style endpoints from another .NET service). For our server-side ASP.NET implementation we hand-write the endpoints — the OpenAPI doc is for *verification* (does our `MapOmni2FaEndpoints()` match the spec?), generated via `NSwag` or `Swashbuckle` and diffed in CI.

### Validation

```bash
npx @redocly/cli lint Core/protocol/omni2fa.openapi.yaml
```

Or render documentation locally:

```bash
npx @redocly/cli preview-docs Core/protocol/omni2fa.openapi.yaml
```

## Why language-neutral?

Cross-stack interoperability is a core promise — React + .NET today, React + Python tomorrow, Angular + .NET the day after. Any code-generation tool that reads OpenAPI works against this file. We avoid framework-specific or language-specific contract formats (no Protobuf for now, no T4 templates, no `*.cs` "shared DTO" packages distributed to clients).
