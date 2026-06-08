using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Internal;

/// <summary>
/// Singleton owning the shared IP-partitioned rate limiter. Holding the limiter here (rather than in
/// the endpoint filter) guarantees every sensitive endpoint shares one window per IP, independent of
/// how the filter instance is created.
/// </summary>
internal sealed class Omni2FaRateLimiter : IDisposable {
    private readonly PartitionedRateLimiter<string> _limiter;

    public bool Enabled { get; }

    public Omni2FaRateLimiter(IOptions<Omni2FaOptions> options) {
        var rateLimit = options.Value.RateLimit;
        Enabled = rateLimit.Enabled;
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions {
                PermitLimit = rateLimit.PermitLimit,
                Window = rateLimit.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    public RateLimitLease Acquire(string partitionKey) {
        return _limiter.AttemptAcquire(partitionKey);
    }

    public void Dispose() {
        _limiter.Dispose();
    }
}
