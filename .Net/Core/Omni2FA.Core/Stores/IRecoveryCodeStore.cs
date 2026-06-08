using Omni2FA.Core.Entities;

namespace Omni2FA.Core.Stores;

/// <summary>Persistence boundary for <see cref="RecoveryCode"/>.</summary>
public interface IRecoveryCodeStore {
    /// <summary>All recovery codes (used and unused) for a user.</summary>
    Task<IReadOnlyList<RecoveryCode>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the user has any recovery codes at all (used or not).</summary>
    Task<bool> HasAnyAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Add a batch of freshly generated codes.</summary>
    Task AddRangeAsync(IEnumerable<RecoveryCode> codes, CancellationToken cancellationToken = default);

    /// <summary>Mark a code as used (stamps <see cref="RecoveryCode.UsedAt"/>).</summary>
    Task MarkUsedAsync(RecoveryCode code, CancellationToken cancellationToken = default);

    /// <summary>Delete all of a user's codes — used on regeneration and when the last method is removed.</summary>
    Task<int> DeleteByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Persist accumulated changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
