using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>List and remove enrolled 2FA methods for a user.</summary>
public interface ITwoFactorMethodService {
    /// <summary>Active methods enrolled by the user, in enrollment order. Empty when none.</summary>
    Task<IReadOnlyList<TwoFactorMethodDto>> ListAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Hard-delete a method belonging to the user. Returns <c>MethodNotFound</c> if missing or not owned.</summary>
    Task<Result> RemoveAsync(string userId, Guid methodId, CancellationToken cancellationToken = default);
}
