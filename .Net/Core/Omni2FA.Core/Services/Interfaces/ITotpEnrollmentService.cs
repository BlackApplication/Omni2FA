using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Two-step TOTP enrollment ceremony — start (generates secret) and confirm (validates first code).</summary>
public interface ITotpEnrollmentService {
    /// <summary>Begin enrollment for the user. <paramref name="accountLabel"/> is rendered in the authenticator app entry name.</summary>
    Task<Result<TotpEnrollStartResponse>> StartAsync(string userId, string accountLabel, CancellationToken cancellationToken = default);

    /// <summary>Confirm the pending enrollment by verifying the user's first authenticator code.</summary>
    Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, TotpEnrollConfirmRequest request, CancellationToken cancellationToken = default);
}
