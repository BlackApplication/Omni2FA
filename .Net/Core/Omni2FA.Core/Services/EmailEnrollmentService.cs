using Omni2FA.Core.Audit;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

/// <summary>Default Email OTP enrollment ceremony — start (emails a code), confirm (validates), resend.</summary>
public class EmailEnrollmentService : IEmailEnrollmentService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITwoFactorChallengeStore _challenges;
    private readonly IEmailOtpService _emailOtp;
    private readonly IRecoveryCodeService _recovery;
    private readonly IOmni2FaAuditSink _audit;

    public EmailEnrollmentService(
        ITwoFactorMethodStore methods,
        ITwoFactorChallengeStore challenges,
        IEmailOtpService emailOtp,
        IRecoveryCodeService recovery,
        IOmni2FaAuditSink audit) {
        _methods = methods;
        _challenges = challenges;
        _emailOtp = emailOtp;
        _recovery = recovery;
        _audit = audit;
    }

    public async Task<Result<EmailEnrollStartResponse>> StartAsync(string userId, EmailEnrollStartRequest request, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(request.Email)) {
            return Result<EmailEnrollStartResponse>.Failure(Omni2FaErrorCodes.ValidationFailed, "An email address is required.");
        }

        var existing = await _methods.GetByTypeAsync(userId, TwoFactorMethodType.Email, activeOnly: true, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result<EmailEnrollStartResponse>.Failure(Omni2FaErrorCodes.TypeAlreadyEnrolled);
        }

        var email = request.Email.Trim();
        var challenge = new TwoFactorChallenge {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = TwoFactorChallengeKind.EnrollEmail,
            EmailAddress = email,
        };
        await _emailOtp.IssueAsync(challenge, email, cancellationToken).ConfigureAwait(false);
        await _challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EmailEnrollStartResponse>.Success(BuildResponse(challenge));
    }

    public async Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, EmailEnrollConfirmRequest request, CancellationToken cancellationToken = default) {
        var challenge = await _challenges.GetActiveAsync(request.EnrollmentId, userId, cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.Kind != TwoFactorChallengeKind.EnrollEmail || challenge.EmailAddress is null) {
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }

        if (!_emailOtp.Verify(challenge, request.Code)) {
            await _challenges.IncrementFailedAttemptsAsync(challenge, cancellationToken).ConfigureAwait(false);
            await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        var method = new TwoFactorMethod {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = TwoFactorMethodType.Email,
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailAddress = challenge.EmailAddress,
        };
        await _methods.AddAsync(method, cancellationToken).ConfigureAwait(false);
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recoveryCodes = await _recovery.GenerateIfNoneAsync(userId, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent {
            Type = Omni2FaAuditEventType.MethodEnrolled,
            UserId = userId,
            MethodType = TwoFactorMethodType.Email,
            MethodId = method.Id,
        }, cancellationToken).ConfigureAwait(false);

        return Result<MethodCreatedResponse>.Success(new MethodCreatedResponse {
            MethodId = method.Id,
            RecoveryCodes = recoveryCodes,
        });
    }

    public async Task<Result<EmailEnrollStartResponse>> ResendAsync(string userId, EmailEnrollResendRequest request, CancellationToken cancellationToken = default) {
        var challenge = await _challenges.GetActiveAsync(request.EnrollmentId, userId, cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.Kind != TwoFactorChallengeKind.EnrollEmail || challenge.EmailAddress is null) {
            return Result<EmailEnrollStartResponse>.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }
        if (DateTime.UtcNow < _emailOtp.ResendAvailableAt(challenge)) {
            return Result<EmailEnrollStartResponse>.Failure(Omni2FaErrorCodes.TooManyAttempts);
        }

        await _emailOtp.IssueAsync(challenge, challenge.EmailAddress, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EmailEnrollStartResponse>.Success(BuildResponse(challenge));
    }

    private EmailEnrollStartResponse BuildResponse(TwoFactorChallenge challenge) {
        return new EmailEnrollStartResponse {
            EnrollmentId = challenge.Id,
            ExpiresAt = challenge.ExpiresAt,
            ResendAvailableAt = _emailOtp.ResendAvailableAt(challenge),
        };
    }
}
