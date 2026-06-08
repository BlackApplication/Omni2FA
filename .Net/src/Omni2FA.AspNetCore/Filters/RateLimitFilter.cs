using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// IP-partitioned fixed-window rate limit for sensitive endpoints. Self-contained (no host
/// <c>UseRateLimiter</c> needed); on rejection emits the standard <c>ErrorResponse</c> envelope with
/// <c>TOO_MANY_ATTEMPTS</c> and a <c>Retry-After</c> header, and raises a <c>RateLimitExceeded</c> audit event.
/// </summary>
internal sealed class RateLimitFilter : IEndpointFilter, IDisposable {
    private readonly PartitionedRateLimiter<string> _limiter;
    private readonly RateLimitOptions _options;

    public RateLimitFilter(IOptions<Omni2FaOptions> options) {
        _options = options.Value.RateLimit;
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions {
                PermitLimit = _options.PermitLimit,
                Window = _options.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        if (!_options.Enabled) {
            return await next(context).ConfigureAwait(false);
        }

        var http = context.HttpContext;
        var partitionKey = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        using var lease = _limiter.AttemptAcquire(partitionKey);
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

    public void Dispose() {
        _limiter.Dispose();
    }
}
