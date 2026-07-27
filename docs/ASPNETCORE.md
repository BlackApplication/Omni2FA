# Omni2FA.AspNetCore — Design Decisions

Concrete choices for the ASP.NET Core adapter (`.Net/src/Omni2FA.AspNetCore`). Captures the *why* behind the code so a future reader doesn't have to re-derive it.

The binding architectural rules (framework-agnostic core, thin adapters) live in [`ARCHITECTURE.md`](ARCHITECTURE.md). This document is one level down — adapter-internal choices.

---

## 1. API style — Minimal API

Endpoints are mapped via a single extension:

```csharp
app.MapOmni2Fa();
```

Internally, the extension creates `IEndpointRouteBuilder.MapGroup("/api/2fa")` and attaches handlers. Group prefix is configurable via `Omni2FaOptions.RoutePrefix`.

**Why not Controllers:**
- Controllers from an external DLL require the host to call `services.AddControllers().AddApplicationPart(typeof(...).Assembly)`. Easy to forget, breaks silently.
- Global filters and conventions registered by the host would apply to our endpoints too — unwanted coupling.
- Minimal API has first-class support for endpoint filters, route groups, and OpenAPI metadata since .NET 7.
- Newer Microsoft libraries (Identity API endpoints in .NET 8+) use Minimal API for the same reasons.

**Layout:**

```
Omni2FA.AspNetCore/
├── Endpoints/
│   ├── MethodsEndpoints.cs       /methods, /methods/{id}
│   ├── ChallengeEndpoints.cs     /challenge/start, /challenge/verify
│   └── EnrollTotpEndpoints.cs    /enroll/totp/start, /enroll/totp/confirm
├── Filters/
│   └── PreAuthFilter.cs          (see §4)
├── Extensions/
│   ├── ServiceCollectionExtensions.cs    AddOmni2Fa(...)
│   ├── EndpointRouteBuilderExtensions.cs MapOmni2Fa(...)
│   └── ResultExtensions.cs               ToHttpResult() (see §5)
└── Omni2FA.AspNetCore.csproj
```

---

## 2. UserId — `string`, not generic

All entities, DTOs, and store interfaces represent the user identifier as `string`.

```csharp
public class TwoFactorMethod {
    public string UserId { get; set; } = string.Empty;
    // ...
}
```

EF maps it to `NVARCHAR(64)`.

**Why not `<TUserId>` generic:**
- Generic is viral: every entity, DTO, store interface, store implementation, EF configuration, and DI registration acquires `<TUserId>`. The integration story ("plug it in, it works") collapses.
- The OpenAPI contract is public and cross-stack. With generic, `userId` becomes `string` for one host and `integer` for another — the contract fragments and the cross-stack guarantee (any frontend works with any backend at the same `MAJOR.MINOR`) breaks.
- Microsoft Identity originally went generic (`IdentityUser<TKey>`) and reversed course — the default `IdentityUser.Id` is `string` for exactly this reason.

**What hosts do:**

```csharp
// Host with Guid userId — one cast on the boundary:
var response = await client.VerifyAsync(...);
var userId = Guid.Parse(response.UserId);

// Host with long userId — same pattern:
var userId = long.Parse(response.UserId);
```

**Migration impact:** a host upgrading from a `Guid UserId` column to Omni2FA needs `ALTER COLUMN UserId TYPE NVARCHAR(64)` (one extra step over a plain `INSERT-SELECT`). Acceptable trade for the simpler library shape.

---

## 3. `IUserContextAccessor` — single interface, configurable default

Host-session endpoints (`/methods/*`, `/enroll/*`) need the current user's id, read from the principal the host's auth middleware put on `HttpContext.User`. We expose this as a single interface.

```csharp
public interface IUserContextAccessor {
    string GetCurrentUserId();
    string GetCurrentUserLabel();
}
```

Default implementation reads configurable claims with a robust fallback:

1. Look up the configured `UserIdClaim` (default `ClaimTypes.NameIdentifier`).
2. If missing, fall back to raw JWT `sub`.

This means **both** common JWT configurations work out of the box:
- Standard `AddJwtBearer(...)` — ASP.NET maps `sub` → `ClaimTypes.NameIdentifier`. Primary lookup hits.
- `AddJwtBearer(o => o.MapInboundClaims = false)` — claims stay as-is, `sub` is `sub`. Fallback hits.

`UserLabelClaim` follows the same pattern (`ClaimTypes.Email` → raw `email` → userId as last resort).

```csharp
public class AspNetCoreOptions {
    public string UserIdClaim { get; set; } = ClaimTypes.NameIdentifier;
    public string UserLabelClaim { get; set; } = ClaimTypes.Email;
    // ...
}
```

Registered via `TryAddSingleton`, so a host that needs to derive the userId differently (custom header, multi-tenant claim, computed from several claims) registers its own implementation:

```csharp
services.AddSingleton<IUserContextAccessor, MyTenantAwareAccessor>();
services.AddOmni2Fa(...);
```

The host registration wins.

**Scope of this interface:** only host-session endpoints. Pre-auth-protected endpoints (`/challenge/*`) read the userId from `HttpContext.Items["Omni2FaUserId"]` set by the pre-auth filter (see §4).

**Pre-auth handler note:** `JwtPreAuthTokenIssuer` validates Omni2FA's own pre-auth tokens with an instance-scoped `JwtSecurityTokenHandler` configured with `MapInboundClaims = false`. The library never mutates the static `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap`, so host JWT claim mapping is untouched.

---

## 4. Pre-auth token — custom endpoint filter, not `AuthenticationScheme`

Pre-auth tokens guarding `/challenge/*` are validated by an `IEndpointFilter`, not by a registered `AuthenticationScheme`.

```csharp
internal sealed class PreAuthFilter : IEndpointFilter {
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var http = context.HttpContext;
        var auth = http.Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
            return PreAuthError(Omni2FaErrorCodes.PreAuthInvalid);
        }
        var issuer = http.RequestServices.GetRequiredService<IPreAuthTokenIssuer>();
        var userId = issuer.ValidateAndGetUserId(auth["Bearer ".Length..]);
        if (userId is null) {
            return PreAuthError(Omni2FaErrorCodes.PreAuthInvalid);
        }
        http.Items["Omni2FaUserId"] = userId;
        return await next(context);
    }
}
```

**Why not `AddJwtBearer("Omni2FaPreAuth", ...)`:**
- The host's auth pipeline (`app.UseAuthentication()`) is a dependency we'd inherit. If it isn't wired, our endpoints break.
- `JwtBearerHandler` writes empty body + `WWW-Authenticate` header on failure. We want our own `ErrorResponse { code, message }` envelope so the frontend can handle errors uniformly.
- A second auth scheme with a similar name/audience is a real source of confusion for users debugging their auth setup.
- We already own `IPreAuthTokenIssuer.ValidateAndGetUserId()` which internally uses `JwtSecurityTokenHandler`. The crypto path is identical.

**Trade-off:** `HttpContext.User` stays anonymous on `/challenge/*`. That's expected — these endpoints don't need a `ClaimsPrincipal`, only the userId, which goes into `HttpContext.Items`.

**Finalize handoff:** `ValidateAndGetUserId` accepts only the *pre-auth* token (`purpose=2fa-pending`), so it proves the password step, not 2FA. To mint the session, the host validates the *verified-handoff* token returned by `challenge/verify` (`VerifySuccessResponse.verifiedToken`, `purpose=2fa-verified`):

```csharp
var userId = preAuth.ValidateVerified(verifiedToken);  // null unless 2FA actually passed
```

Both tokens share signing/issuer/audience and differ only by purpose, so neither can stand in for the other. The host needs no audit-event listener or cache to know the ceremony passed — the token is the signal, identical for code, passkey, and recovery-code logins.

---

## 5. Result → IResult — explicit extension, not middleware

Services return `Result` / `Result<T>`. Endpoints map them to HTTP responses via extension methods:

```csharp
internal static class ResultExtensions {
    public static IResult ToHttpResult(this Result result) {
        if (result.IsSuccess) return Results.NoContent();
        return ErrorResult(result.ErrorCode!, result.ErrorMessage);
    }

    public static IResult ToHttpResult<T>(this Result<T> result) {
        if (result.IsSuccess) return Results.Ok(result.Value);
        return ErrorResult(result.ErrorCode!, result.ErrorMessage);
    }

    private static IResult ErrorResult(string code, string? message) {
        var status = code switch {
            Omni2FaErrorCodes.ValidationFailed => 400,
            Omni2FaErrorCodes.InvalidCode or
            Omni2FaErrorCodes.PreAuthExpired or
            Omni2FaErrorCodes.PreAuthInvalid or
            Omni2FaErrorCodes.ChallengeConsumed or
            Omni2FaErrorCodes.WebAuthnVerificationFailed or
            Omni2FaErrorCodes.RecoveryCodeInvalid or
            Omni2FaErrorCodes.RecoveryCodeUsed => 401,
            Omni2FaErrorCodes.MethodNotFound or
            Omni2FaErrorCodes.ChallengeNotFound => 404,
            Omni2FaErrorCodes.TypeAlreadyEnrolled or
            Omni2FaErrorCodes.LastMethodProtected or
            Omni2FaErrorCodes.MaxMethodsReached => 409,
            Omni2FaErrorCodes.TooManyAttempts => 429,
            _ => 500,
        };
        return Results.Json(new ErrorResponse { Code = code, Message = message }, statusCode: status);
    }
}
```

Endpoints look like:

```csharp
group.MapPost("/enroll/totp/start", async (IEnrollmentService svc, IUserContextAccessor user, CancellationToken ct) =>
{
    var result = await svc.StartTotpEnrollmentAsync(user.GetCurrentUserId(), ct);
    return result.ToHttpResult();
});
```

**Why not exception-based middleware:**
- Expected failures (invalid OTP, expired challenge) aren't exceptional — they're outcomes. Using exceptions for control flow inflates stacktraces, breaks profilers, and obscures real errors.
- The mapping is small and stable; a switch in one file is easier to audit than a chain of `IExceptionHandler`s.

The HTTP-status switch is the single source of truth for status codes. The error-code catalogue ([`Core/protocol/ERROR_CODES.md`](../Core/protocol/ERROR_CODES.md)) lists the same codes; a unit test asserts every code in `Omni2FaErrorCodes` is mapped.

> `STEP_UP_REQUIRED` (403) is the one exception — it's emitted directly by the step-up filters (§6), not through `ToHttpResult`, because it's a gate decision rather than a service `Result`.

---

## 6. Step-up — endpoint/action filters over an authorization policy

Step-up 2FA gates a host's **own** sensitive endpoints (change password, view recovery codes, remove a method). Two thin filters share one decision:

- `[RequireTwoFactor]` — an `IAsyncActionFilter` attribute for MVC controllers (the `[Authorize]`-style entry point).
- `.RequireStepUp()` — adds an `IEndpointFilter` to minimal-API endpoints/groups (mirrors `PreAuthFilter` / `RateLimitFilter`).

Both call `StepUpGate.EvaluateAsync`, which resolves the current user (`IUserContextAccessor`) and the `X-Omni2FA-StepUp` header, then asks `IStepUpEvaluator` (in `Omni2FA.Core`) for a verdict: `Satisfied` / `NotEnrolledBypass` → proceed, or `Required` → `403 STEP_UP_REQUIRED` with `details.availableMethods` + `stepUpPath`.

**Why filters, not an authorization policy / `IAuthorizationRequirement`:**
- A policy can only fail to a bare 403/401. To return the rich `STEP_UP_REQUIRED` envelope (which methods, where to confirm) you'd have to replace the global `IAuthorizationMiddlewareResultHandler` — which rewrites *every* authorization failure in the host. The library should answer for its own barrier, not take over the host's auth pipeline.
- Filters are local to the decorated endpoint, need no host policy registration, and match the adapter's existing endpoint-filter style.
- Step-up is orthogonal to authorization ("is this a fresh 2FA?" vs "who are you / may you?"), so it composes on top of `[Authorize]` rather than inside it.

**Shared core.** All the security logic (token validity, identity binding, single-use consume, enrollment check) lives in `IStepUpEvaluator`, so both filters are dumb wrappers and the decision is testable without HTTP.

**Step-up endpoints.** `/stepup/start|resend|verify` are session-authenticated (`RequireAuthorization()`) mirrors of `/challenge/*` — the user comes from `IUserContextAccessor`, not a pre-auth token. `verify` calls `ITwoFactorChallengeService.VerifyStepUpAsync`, which reuses the login verification core but mints a step-up token (`purpose=2fa-stepup`) instead of the login handoff token.

**Single-use transport.** The token is a stateless JWT, but single use needs a little server state: `IStepUpNonceStore` records spent token ids until they expire — without it a token would be replayable until its TTL elapsed. The default `InMemoryStepUpNonceStore` (`IMemoryCache`) is single-instance; register a shared store for multi-node. The evaluator consumes the id only after confirming the token's subject matches the caller, so a foreign token is never burned on someone else's behalf.

**Protecting Omni2FA's own endpoints (v0.8.0).** Hosts decorate their own endpoints, but the destructive endpoints `MapOmni2Fa` mounts can't be reached with an attribute, so they're gated by opt-in flags: `StepUp.RequireTwoFactorToEnroll` → `/enroll/*/start`, `RequireTwoFactorToRemoveMethod` → `DELETE /methods/{id}`, `RequireTwoFactorToRegenerateRecoveryCodes` → `/recovery-codes/regenerate` (all default `false`). `MapOmni2Fa` reads the flags and conditionally chains `.RequireStepUp()`. Enroll is gated at `start` (the entry point), so the 403 lands before an email is sent or WebAuthn options are issued. Because these are the library's own calls, the frontend retry lives in the client (`setStepUpHandler`), not in each hook.

---

## 7. Multiple audiences — one mount per population

Some hosts authenticate more than one population: staff signing in at `/api/auth`, customers at
`/api/portal/auth`, separate identity tables, separate sessions, ids that overlap between them. Before
v0.10.0 the library had a single mount and a single subject space, so those hosts had to improvise:
flag the shared `/api/2fa` requests with a header so the backend knew which cookie to read, and prefix
subjects by hand in every place that talked to Omni2FA. Both are now first-class.

```csharp
services.AddOmni2Fa(o => {
    o.AspNetCore.Audiences.Add(new Omni2FaAudienceOptions {
        Name = "customer",
        RoutePrefix = "/api/portal/2fa",     // under the path that already identifies the population
        SubjectPrefix = "customer:",         // customer 42 → "customer:42"; staff 42 stays "42"
        AuthenticationSchemes = "CustomerCookie",
    });
});

app.MapOmni2Fa();              // staff  → /api/2fa
app.MapOmni2Fa("customer");    // portal → /api/portal/2fa

// The host's own step-up-gated endpoints declare which population they serve:
[Omni2FaAudience("customer")]
public sealed class CustomerCardsController : ControllerBase { … }        // MVC
app.MapGroup("/api/portal").WithOmni2FaAudience("customer");              // minimal API
```

**An audience is (route prefix, subject namespace, auth scheme).** Those three always travel together —
splitting them is what produced the header workaround in the first place. Mounting under the path that
already identifies the population means the host's existing routing rules (cookie selection, CORS, an
auth scheme chosen by path) cover the 2FA endpoints for free, with nothing for the frontend to remember.

**The namespace is applied by the library, not the host.** The mount tags its endpoints with the
audience name; `UserContextAccessor` reads that metadata and prefixes the id from the principal. Hosts
overriding the accessor override `GetRawUserId()` and still get namespacing. Where the host talks to
Omni2FA outside a mounted endpoint — issuing the pre-auth token at login, reading the subject back out
of a verified-handoff token at finalize — `IOmni2FaAudienceRegistry.ToSubject` / `TryGetUserId` apply the
same rule, so the prefix is never open-coded. `TryGetUserId` on the *default* audience rejects subjects
carrying another audience's prefix: without that, `"customer:42"` would pass as a staff id.

**Why endpoint metadata rather than a per-request resolver.** The audience is a property of the endpoint
(this route serves customers), not of the request, so it is known at mapping time and cannot drift. It
also travels to the host's own endpoints through an attribute — needed by `[RequireTwoFactor]`, which
otherwise evaluates step-up against the wrong subject and would never match the caller's methods.

**Per-audience scheme lives in options, not on the returned group.** `MapOmni2Fa` returns the
`RouteGroupBuilder` so hosts can attach their own conventions, but chaining `.RequireAuthorization(…)`
there would also cover `/challenge/*` — which runs *before* a session exists and would break login.
`AuthenticationSchemes` / `AuthorizationPolicy` on the audience are applied only to the endpoints that
already require a session.

**Startup validation.** Names, route prefixes, and subject prefixes must be unique, and every audience
past the default must set a route prefix. All four are `ValidateOnStart` checks: a duplicate subject
prefix silently merges two populations' 2FA methods, which is not something to discover in production.

**Single-population hosts are unaffected.** The default audience is implicit — mounted at
`AspNetCore.RoutePrefix`, no namespace, ids stored exactly as before. `MapOmni2Fa()` with no argument
keeps its old behaviour, and endpoint names stay bare (`listMethods`); only additional mounts suffix
theirs (`listMethods-customer`), since endpoint names must be unique application-wide.
