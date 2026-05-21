using Microsoft.EntityFrameworkCore;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Stores;

/// <summary>
/// EF Core implementation of <see cref="ITwoFactorChallengeStore"/>. Works against any
/// <see cref="DbContext"/> that has called <c>modelBuilder.ApplyOmni2FaConfiguration()</c>
/// during <c>OnModelCreating</c>.
/// </summary>
public class TwoFactorChallengeStore : ITwoFactorChallengeStore {
    private readonly DbContext _context;

    public TwoFactorChallengeStore(DbContext context) {
        _context = context;
    }

    private DbSet<TwoFactorChallenge> Set => _context.Set<TwoFactorChallenge>();

    public Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default) {
        if (challenge.CreatedAt == default) {
            challenge.CreatedAt = DateTime.UtcNow;
        }
        Set.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<TwoFactorChallenge?> GetByIdAsync(Guid challengeId, CancellationToken cancellationToken = default) {
        return Set.FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);
    }

    public Task<TwoFactorChallenge?> GetActiveAsync(Guid challengeId, string userId, CancellationToken cancellationToken = default) {
        var now = DateTime.UtcNow;
        return Set.FirstOrDefaultAsync(c => c.Id == challengeId && c.UserId == userId && c.ConsumedAt == null && c.ExpiresAt > now, cancellationToken);
    }

    public Task MarkConsumedAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default) {
        challenge.ConsumedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task IncrementFailedAttemptsAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default) {
        challenge.FailedAttempts += 1;
        return Task.CompletedTask;
    }

    public Task<int> PurgeExpiredAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) {
        return Set
            .Where(c => c.ExpiresAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
