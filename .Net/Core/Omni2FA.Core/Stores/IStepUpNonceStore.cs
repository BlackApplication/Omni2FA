namespace Omni2FA.Core.Stores;

/// <summary>
/// Records consumed step-up token ids so each token satisfies exactly one action. Step-up tokens are
/// otherwise stateless JWTs — without this they would be replayable until expiry. The default adapter
/// keeps ids in memory; multi-instance hosts replace it with a shared store (e.g. Redis) so a token
/// spent on one node cannot be reused on another.
/// </summary>
public interface IStepUpNonceStore {
    /// <summary>
    /// Atomically mark <paramref name="jti"/> as consumed. Returns <c>true</c> if it had not been seen
    /// before (this call wins the single use); <c>false</c> if it was already consumed or already expired.
    /// <paramref name="expiresAtUtc"/> is the token's expiry — the id only needs retaining until then.
    /// </summary>
    Task<bool> TryConsumeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
}
