using Omni2FA.Core.Entities;

namespace Omni2FA.Core.Stores;

/// <summary>Persistence boundary for <see cref="TwoFactorChallenge"/>.</summary>
public interface ITwoFactorChallengeStore {
    /// <summary>Add a new challenge.</summary>
    Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Find a challenge by id. Returns null if missing.</summary>
    Task<TwoFactorChallenge?> GetByIdAsync(Guid challengeId, CancellationToken cancellationToken = default);

    /// <summary>Find an unconsumed, unexpired challenge for the user. Returns null if none active.</summary>
    Task<TwoFactorChallenge?> GetActiveAsync(Guid challengeId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find the active (unconsumed, unexpired) login challenge for a given method and user.
    /// Used by out-of-band methods (Email) where the challenge is keyed by method rather than
    /// by a client-held challenge id. Returns null if none active.
    /// </summary>
    Task<TwoFactorChallenge?> GetActiveLoginChallengeAsync(string userId, Guid methodId, CancellationToken cancellationToken = default);

    /// <summary>Mark a challenge as consumed (stamps <see cref="TwoFactorChallenge.ConsumedAt"/>).</summary>
    Task MarkConsumedAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Increment the failed-attempts counter.</summary>
    Task IncrementFailedAttemptsAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Remove rows where <c>ExpiresAt</c> is past <paramref name="cutoffUtc"/>. Returns count deleted.</summary>
    Task<int> PurgeExpiredAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>Persist accumulated changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
