using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Generation, regeneration, verification, and wiping of one-time recovery codes.</summary>
public interface IRecoveryCodeService {
    /// <summary>Generate a set only if the user currently has none (i.e. this is their first method). Returns the plaintext codes, or null if they already had some.</summary>
    Task<IReadOnlyList<string>?> GenerateIfNoneAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Wipe and re-issue a fresh set, returning the new plaintext codes.</summary>
    Task<Result<RecoveryCodesResponse>> RegenerateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Verify and consume a single recovery code as a login fallback.</summary>
    Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, RecoveryCodeVerifyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete all of a user's recovery codes (e.g. when their last method is removed).</summary>
    Task WipeAsync(string userId, CancellationToken cancellationToken = default);
}
