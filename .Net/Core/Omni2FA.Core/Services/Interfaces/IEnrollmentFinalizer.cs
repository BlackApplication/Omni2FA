using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Shared tail of every enrollment confirm: mint recovery codes on the user's first method and raise
/// the <c>MethodEnrolled</c> audit event, returning the response. Keeps that logic in one place across
/// the TOTP / Email / WebAuthn enrollment services.
/// </summary>
public interface IEnrollmentFinalizer {
    /// <summary>Run the post-persist steps for a freshly added method and build its <see cref="MethodCreatedResponse"/>.</summary>
    Task<MethodCreatedResponse> FinalizeAsync(string userId, TwoFactorMethod method, CancellationToken cancellationToken = default);
}
