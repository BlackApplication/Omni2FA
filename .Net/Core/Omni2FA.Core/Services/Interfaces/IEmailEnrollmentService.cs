using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>Two-step Email OTP enrollment — start (emails a code), confirm (validates), resend (re-emails).</summary>
public interface IEmailEnrollmentService {
    /// <summary>Begin enrollment for <paramref name="userId"/>, emailing a code to the host-supplied address.</summary>
    Task<Result<EmailEnrollStartResponse>> StartAsync(string userId, EmailEnrollStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Confirm the pending enrollment by verifying the emailed code, persisting the method on success.</summary>
    Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, EmailEnrollConfirmRequest request, CancellationToken cancellationToken = default);

    /// <summary>Re-send the enrollment code, subject to the resend cooldown.</summary>
    Task<Result<EmailEnrollStartResponse>> ResendAsync(string userId, EmailEnrollResendRequest request, CancellationToken cancellationToken = default);
}
