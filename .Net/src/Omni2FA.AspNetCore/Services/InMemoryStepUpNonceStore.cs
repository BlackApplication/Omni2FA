using Microsoft.Extensions.Caching.Memory;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.Services;

/// <summary>
/// Default <see cref="IStepUpNonceStore"/> backed by <see cref="IMemoryCache"/>. Each consumed token id
/// is held until the token's own expiry, so the footprint is bounded by the number of step-up tokens
/// issued within a TTL window. Single-instance only — multi-node hosts must register a shared store so a
/// token spent on one node is rejected on the others.
/// </summary>
internal sealed class InMemoryStepUpNonceStore : IStepUpNonceStore {
    private const string KeyPrefix = "Omni2Fa:StepUpNonce:";
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public InMemoryStepUpNonceStore(IMemoryCache cache) {
        _cache = cache;
    }

    public Task<bool> TryConsumeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) {
        if (expiresAtUtc <= DateTime.UtcNow) {
            return Task.FromResult(false);
        }
        var key = KeyPrefix + jti;
        lock (_gate) {
            if (_cache.TryGetValue(key, out _)) {
                return Task.FromResult(false);
            }
            _cache.Set(key, true, expiresAtUtc);
        }
        return Task.FromResult(true);
    }
}
