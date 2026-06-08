using Microsoft.Extensions.Options;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TotpEnrollmentService : ITotpEnrollmentService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITwoFactorChallengeStore _challenges;
    private readonly ITotpService _totp;
    private readonly ISecretProtector _protector;
    private readonly IRecoveryCodeService _recovery;
    private readonly IOmni2FaAuditSink _audit;
    private readonly TimeSpan _enrollmentTtl;

    public TotpEnrollmentService(
        ITwoFactorMethodStore methods,
        ITwoFactorChallengeStore challenges,
        ITotpService totp,
        ISecretProtector protector,
        IRecoveryCodeService recovery,
        IOmni2FaAuditSink audit,
        IOptions<Omni2FaOptions> options) {
        _methods = methods;
        _challenges = challenges;
        _totp = totp;
        _protector = protector;
        _recovery = recovery;
        _audit = audit;
        _enrollmentTtl = options.Value.AspNetCore.EnrollmentTtl;
    }

    public async Task<Result<TotpEnrollStartResponse>> StartAsync(string userId, string accountLabel, CancellationToken cancellationToken = default) {
        var existing = await _methods.GetByTypeAsync(userId, TwoFactorMethodType.Totp, activeOnly: true, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result<TotpEnrollStartResponse>.Failure(Omni2FaErrorCodes.TypeAlreadyEnrolled);
        }

        var secret = _totp.GenerateSecret();
        var protectedSecret = _protector.Protect(secret);
        var otpAuthUri = _totp.BuildOtpAuthUri(accountLabel, secret);

        var now = DateTime.UtcNow;
        var challenge = new TwoFactorChallenge {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = TwoFactorChallengeKind.EnrollTotp,
            TotpSecretCandidate = protectedSecret,
            CreatedAt = now,
            ExpiresAt = now.Add(_enrollmentTtl),
        };
        await _challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TotpEnrollStartResponse>.Success(new TotpEnrollStartResponse {
            EnrollmentId = challenge.Id,
            OtpAuthUri = otpAuthUri,
            Secret = secret,
        });
    }

    public async Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, TotpEnrollConfirmRequest request, CancellationToken cancellationToken = default) {
        var challenge = await _challenges.GetActiveAsync(request.EnrollmentId, userId, cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.Kind != TwoFactorChallengeKind.EnrollTotp || challenge.TotpSecretCandidate is null) {
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }

        var secret = _protector.Unprotect(challenge.TotpSecretCandidate);
        if (!_totp.ValidateCode(secret, request.Code)) {
            await _challenges.IncrementFailedAttemptsAsync(challenge, cancellationToken).ConfigureAwait(false);
            await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        var method = new TwoFactorMethod {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = TwoFactorMethodType.Totp,
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TotpSecret = challenge.TotpSecretCandidate,
        };
        await _methods.AddAsync(method, cancellationToken).ConfigureAwait(false);
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recoveryCodes = await _recovery.GenerateIfNoneAsync(userId, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent {
            Type = Omni2FaAuditEventType.MethodEnrolled,
            UserId = userId,
            MethodType = TwoFactorMethodType.Totp,
            MethodId = method.Id,
        }, cancellationToken).ConfigureAwait(false);

        return Result<MethodCreatedResponse>.Success(new MethodCreatedResponse {
            MethodId = method.Id,
            RecoveryCodes = recoveryCodes,
        });
    }
}
