using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Login-time 2FA ceremony — start a challenge for a chosen method and verify the user's response.</summary>
public interface ITwoFactorChallengeService {
    /// <summary>Prepare verification of the picked method. For TOTP — a no-op acknowledgement; for Email — issues and sends an OTP.</summary>
    Task<Result<ChallengeStartResponse>> StartAsync(string userId, ChallengeStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Re-send the login OTP for a delivery-based method (Email), subject to a resend cooldown.</summary>
    Task<Result<ChallengeStartResponse>> ResendAsync(string userId, ChallengeResendRequest request, CancellationToken cancellationToken = default);

    /// <summary>Final step — validate the user's code/assertion against the picked method.</summary>
    Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Step-up variant of <see cref="VerifyAsync"/> for an already-authenticated user confirming a
    /// sensitive action. Runs the same method verification but mints a single-use step-up token
    /// instead of the login-handoff token.
    /// </summary>
    Task<Result<StepUpVerifyResponse>> VerifyStepUpAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default);
}
