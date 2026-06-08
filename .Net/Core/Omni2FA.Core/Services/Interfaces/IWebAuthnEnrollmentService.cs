using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Two-step WebAuthn enrollment ceremony — start (issues creation options) and confirm (verifies the attestation).</summary>
public interface IWebAuthnEnrollmentService {
    /// <summary>Begin enrollment for the user. <paramref name="accountLabel"/> is shown by the authenticator / passkey manager.</summary>
    Task<Result<WebAuthnEnrollStartResponse>> StartAsync(string userId, string accountLabel, CancellationToken cancellationToken = default);

    /// <summary>Confirm enrollment by verifying the browser attestation, persisting the credential on success.</summary>
    Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, WebAuthnEnrollConfirmRequest request, CancellationToken cancellationToken = default);
}
