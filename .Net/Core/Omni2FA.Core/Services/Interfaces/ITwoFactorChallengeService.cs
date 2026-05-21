using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Login-time 2FA ceremony — start a challenge for a chosen method and verify the user's response.</summary>
public interface ITwoFactorChallengeService {
    /// <summary>Prepare verification of the picked method. For TOTP — a no-op acknowledgement.</summary>
    Task<Result<ChallengeStartResponse>> StartAsync(string userId, ChallengeStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Final step — validate the user's code/assertion against the picked method.</summary>
    Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default);
}
