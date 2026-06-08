using Omni2FA.Core.Entities;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Email OTP primitive — issues a code onto a challenge and emails it, and verifies a submitted code.
/// Operates on a <see cref="TwoFactorChallenge"/> entity supplied by the caller; persistence (add,
/// consume, save) is the orchestrator's responsibility.
/// </summary>
public interface IEmailOtpService {
    /// <summary>
    /// Generate a fresh code, stamp <see cref="TwoFactorChallenge.EmailOtpHash"/>,
    /// <see cref="TwoFactorChallenge.CreatedAt"/> and <see cref="TwoFactorChallenge.ExpiresAt"/>,
    /// then email the code to <paramref name="email"/>.
    /// </summary>
    Task IssueAsync(TwoFactorChallenge challenge, string email, CancellationToken cancellationToken = default);

    /// <summary>True if <paramref name="code"/> matches the challenge's stored hash and the challenge has not expired.</summary>
    bool Verify(TwoFactorChallenge challenge, string code);

    /// <summary>UTC instant at which a resend becomes permitted for the given challenge (based on its last issue time).</summary>
    DateTime ResendAvailableAt(TwoFactorChallenge challenge);
}
