using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TwoFactorChallengeService : ITwoFactorChallengeService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITwoFactorChallengeStore _challenges;
    private readonly ITotpService _totp;
    private readonly ISecretProtector _protector;
    private readonly IEmailOtpService _emailOtp;

    public TwoFactorChallengeService(
        ITwoFactorMethodStore methods,
        ITwoFactorChallengeStore challenges,
        ITotpService totp,
        ISecretProtector protector,
        IEmailOtpService emailOtp) {
        _methods = methods;
        _challenges = challenges;
        _totp = totp;
        _protector = protector;
        _emailOtp = emailOtp;
    }

    public async Task<Result<ChallengeStartResponse>> StartAsync(string userId, ChallengeStartRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }

        if (method.Type == TwoFactorMethodType.Email) {
            return await StartEmailAsync(userId, method, cancellationToken).ConfigureAwait(false);
        }

        return Result<ChallengeStartResponse>.Success(new ChallengeStartResponse {
            Type = method.Type,
        });
    }

    public async Task<Result<ChallengeStartResponse>> ResendAsync(string userId, ChallengeResendRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }
        if (method.Type != TwoFactorMethodType.Email || method.EmailAddress is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.ValidationFailed, "This method does not support resending codes.");
        }

        var challenge = await _challenges.GetActiveLoginChallengeAsync(userId, method.Id, cancellationToken).ConfigureAwait(false);
        if (challenge is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }
        if (DateTime.UtcNow < _emailOtp.ResendAvailableAt(challenge)) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.TooManyAttempts);
        }

        await _emailOtp.IssueAsync(challenge, method.EmailAddress, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ChallengeStartResponse>.Success(EmailResponse(challenge));
    }

    public async Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }

        var verified = method.Type switch {
            TwoFactorMethodType.Totp => VerifyTotp(method, request.Code),
            TwoFactorMethodType.Email => await VerifyEmailAsync(userId, method, request.Code, cancellationToken).ConfigureAwait(false),
            _ => false,
        };
        if (!verified) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        await _methods.MarkUsedAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerifySuccessResponse>.Success(new VerifySuccessResponse {
            Verified = true,
            UserId = userId,
        });
    }

    private async Task<Result<ChallengeStartResponse>> StartEmailAsync(string userId, TwoFactorMethod method, CancellationToken cancellationToken) {
        if (method.EmailAddress is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.ValidationFailed, "The Email method has no destination address.");
        }

        var existing = await _challenges.GetActiveLoginChallengeAsync(userId, method.Id, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            // Within cooldown — don't re-send, just report the live challenge to the client.
            if (DateTime.UtcNow < _emailOtp.ResendAvailableAt(existing)) {
                return Result<ChallengeStartResponse>.Success(EmailResponse(existing));
            }
            await _emailOtp.IssueAsync(existing, method.EmailAddress, cancellationToken).ConfigureAwait(false);
            await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<ChallengeStartResponse>.Success(EmailResponse(existing));
        }

        var challenge = new TwoFactorChallenge {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = TwoFactorChallengeKind.Login,
            MethodId = method.Id,
            EmailAddress = method.EmailAddress,
        };
        await _emailOtp.IssueAsync(challenge, method.EmailAddress, cancellationToken).ConfigureAwait(false);
        await _challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ChallengeStartResponse>.Success(EmailResponse(challenge));
    }

    private bool VerifyTotp(TwoFactorMethod method, string code) {
        if (method.TotpSecret is null) {
            return false;
        }
        var secret = _protector.Unprotect(method.TotpSecret);
        return _totp.ValidateCode(secret, code);
    }

    private async Task<bool> VerifyEmailAsync(string userId, TwoFactorMethod method, string code, CancellationToken cancellationToken) {
        var challenge = await _challenges.GetActiveLoginChallengeAsync(userId, method.Id, cancellationToken).ConfigureAwait(false);
        if (challenge is null) {
            return false;
        }
        if (!_emailOtp.Verify(challenge, code)) {
            await _challenges.IncrementFailedAttemptsAsync(challenge, cancellationToken).ConfigureAwait(false);
            await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private ChallengeStartResponse EmailResponse(TwoFactorChallenge challenge) {
        return new ChallengeStartResponse {
            Type = TwoFactorMethodType.Email,
            ExpiresAt = challenge.ExpiresAt,
            ResendAvailableAt = _emailOtp.ResendAvailableAt(challenge),
        };
    }
}
