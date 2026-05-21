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
}
```

Default implementation reads a configurable claim:

```csharp
public class Omni2FaOptions {
    public string UserIdClaim { get; set; } = ClaimTypes.NameIdentifier;
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
