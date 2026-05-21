using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Stores;

/// <summary>
/// Persistence boundary for <see cref="TwoFactorMethod"/>. Implemented by storage adapters;
/// core code never touches a database directly.
/// </summary>
public interface ITwoFactorMethodStore {
    /// <summary>List all active methods enrolled by the given user, in enrollment order.</summary>
    Task<IReadOnlyList<TwoFactorMethod>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the user has at least one active method.</summary>
    Task<bool> HasActiveMethodsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Find an active method by id, scoped to the given user. Returns null if missing or inactive.</summary>
    Task<TwoFactorMethod?> GetActiveAsync(Guid methodId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Find a method of the given type for a user.</summary>
    Task<TwoFactorMethod?> GetByTypeAsync(Guid userId, TwoFactorMethodType type, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>Add a new method. Implementations stamp <see cref="TwoFactorMethod.CreatedAt"/> if unset.</summary>
    Task AddAsync(TwoFactorMethod method, CancellationToken cancellationToken = default);

    /// <summary>Remove a method (hard delete).</summary>
    Task RemoveAsync(TwoFactorMethod method, CancellationToken cancellationToken = default);

    /// <summary>Update <see cref="TwoFactorMethod.LastUsedAt"/> to the current UTC time.</summary>
    Task MarkUsedAsync(TwoFactorMethod method, CancellationToken cancellationToken = default);

    /// <summary>Persist accumulated changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
