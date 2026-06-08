using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Omni2FA.AspNetCore.Internal;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// IP-partitioned rate limit for sensitive endpoints. Self-contained (no host <c>UseRateLimiter</c>);
/// on rejection emits the standard <c>ErrorResponse</c> with <c>TOO_MANY_ATTEMPTS</c> + a <c>Retry-After</c>
/// header and raises a <c>RateLimitExceeded</c> audit event. The shared window lives in the singleton
/// <see cref="Omni2FaRateLimiter"/>, resolved per request — so this filter is instantiation-agnostic.
///
/// Behind a reverse proxy / load balancer, configure ASP.NET forwarded-headers so
/// <c>RemoteIpAddress</c> reflects the real client; otherwise all clients share one partition.
/// </summary>
internal sealed class RateLimitFilter : IEndpointFilter {
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var http = context.HttpContext;
        var limiter = http.RequestServices.GetRequiredService<Omni2FaRateLimiter>();
        if (!limiter.Enabled) {
            return await next(context).ConfigureAwait(false);
        }

        var partitionKey = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        using var lease = limiter.Acquire(partitionKey);
        if (lease.IsAcquired) {
            return await next(context).ConfigureAwait(false);
        }

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }
        var audit = http.RequestServices.GetRequiredService<IOmni2FaAuditSink>();
        await audit.RecordAsync(new Omni2FaAuditEvent { Type = Omni2FaAuditEventType.RateLimitExceeded, Detail = partitionKey }, http.RequestAborted).ConfigureAwait(false);

        return Results.Json(
            new ErrorResponse { Code = Omni2FaErrorCodes.TooManyAttempts, Message = "Too many attempts. Please wait and try again." },
            statusCode: StatusCodes.Status429TooManyRequests);
    }
}
