using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Stores;

/// <summary>
/// Convenience extensions over <see cref="ITwoFactorChallengeStore"/> that bundle the
/// load-and-guard and write-then-save pairs repeated across the enrollment/challenge services.
/// </summary>
public static class ChallengeStoreExtensions {
    /// <summary>
    /// Returns the active (unconsumed, unexpired) challenge for <paramref name="enrollmentId"/> only when
    /// its <see cref="TwoFactorChallenge.Kind"/> matches <paramref name="kind"/>; otherwise <c>null</c>.
    /// Centralizes the "right challenge, right ceremony" guard so callers check only the payload they need.
    /// </summary>
    public static async Task<TwoFactorChallenge?> GetActiveEnrollmentAsync(
        this ITwoFactorChallengeStore challenges, Guid enrollmentId, string userId,
        TwoFactorChallengeKind kind, CancellationToken cancellationToken = default) {
        var challenge = await challenges.GetActiveAsync(enrollmentId, userId, cancellationToken).ConfigureAwait(false);
        return challenge is not null && challenge.Kind == kind ? challenge : null;
    }

    /// <summary>Increment the failed-attempts counter and persist it.</summary>
    public static async Task RecordFailedAttemptAsync(
        this ITwoFactorChallengeStore challenges, TwoFactorChallenge challenge, CancellationToken cancellationToken = default) {
        await challenges.IncrementFailedAttemptsAsync(challenge, cancellationToken).ConfigureAwait(false);
        await challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Add a new challenge and persist it.</summary>
    public static async Task AddAndSaveAsync(
        this ITwoFactorChallengeStore challenges, TwoFactorChallenge challenge, CancellationToken cancellationToken = default) {
        await challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
