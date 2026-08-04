using System.Text;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Services.WebAuthn;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TwoFactorChallengeService : ITwoFactorChallengeService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITwoFactorChallengeStore _challenges;
    private readonly ITotpService _totp;
    private readonly ISecretProtector _protector;
    private readonly IEmailOtpService _emailOtp;
    private readonly IWebAuthnCeremonyService _webAuthn;
    private readonly IOmni2FaAuditSink _audit;
    private readonly IPreAuthTokenIssuer _preAuth;
    private readonly TimeSpan _webAuthnTtl;
    private readonly TimeSpan _graceWindow;

    public TwoFactorChallengeService(
        ITwoFactorMethodStore methods,
        ITwoFactorChallengeStore challenges,
        ITotpService totp,
        ISecretProtector protector,
        IEmailOtpService emailOtp,
        IWebAuthnCeremonyService webAuthn,
        IOmni2FaAuditSink audit,
        IPreAuthTokenIssuer preAuth,
        IOptions<Omni2FaOptions> options) {
        _methods = methods;
        _challenges = challenges;
        _totp = totp;
        _protector = protector;
        _emailOtp = emailOtp;
        _webAuthn = webAuthn;
        _audit = audit;
        _preAuth = preAuth;
        _webAuthnTtl = options.Value.AspNetCore.EnrollmentTtl;
        _graceWindow = options.Value.StepUp.GraceWindow;
    }

    public async Task<Result<ChallengeStartResponse>> StartAsync(string userId, ChallengeStartRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }

        return method.Type switch {
            TwoFactorMethodType.Email => await StartEmailAsync(userId, method, cancellationToken).ConfigureAwait(false),
            TwoFactorMethodType.WebAuthn => await StartWebAuthnAsync(userId, method, cancellationToken).ConfigureAwait(false),
            _ => Result<ChallengeStartResponse>.Success(new ChallengeStartResponse { Type = method.Type }),
        };
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
        var verified = await VerifyInternalAsync(
            userId, request,
            Omni2FaAuditEventType.LoginVerifySucceeded,
            Omni2FaAuditEventType.LoginVerifyFailed,
            cancellationToken).ConfigureAwait(false);
        if (verified.IsFailure) {
            return Result<VerifySuccessResponse>.Failure(verified.ErrorCode!, verified.ErrorMessage);
        }

        var handoff = _preAuth.IssueVerified(userId);
        if (_graceWindow <= TimeSpan.Zero) {
            return Result<VerifySuccessResponse>.Success(new VerifySuccessResponse {
                UserId = userId,
                VerifiedToken = handoff.Token,
                ExpiresAt = handoff.ExpiresAt,
            });
        }

        // The login ceremony is a 2FA proof too — carry it into the barrier so the first protected page
        // does not ask again seconds later.
        var stepUp = _preAuth.IssueStepUp(userId);
        return Result<VerifySuccessResponse>.Success(new VerifySuccessResponse {
            UserId = userId,
            VerifiedToken = handoff.Token,
            ExpiresAt = handoff.ExpiresAt,
            StepUpToken = stepUp.Token,
            StepUpGraceUntil = DateTime.UtcNow.Add(_graceWindow),
        });
    }

    public async Task<Result<StepUpVerifyResponse>> VerifyStepUpAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default) {
        var verified = await VerifyInternalAsync(
            userId, request,
            Omni2FaAuditEventType.StepUpVerifySucceeded,
            Omni2FaAuditEventType.StepUpVerifyFailed,
            cancellationToken).ConfigureAwait(false);
        if (verified.IsFailure) {
            return Result<StepUpVerifyResponse>.Failure(verified.ErrorCode!, verified.ErrorMessage);
        }

        var token = _preAuth.IssueStepUp(userId);
        return Result<StepUpVerifyResponse>.Success(new StepUpVerifyResponse {
            StepUpToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            GraceUntil = _graceWindow > TimeSpan.Zero ? DateTime.UtcNow.Add(_graceWindow) : null,
        });
    }

    /// <summary>
    /// Shared verification core for login and step-up: validate the picked method's code/assertion,
    /// mark the method used, and audit. Mints no token — callers mint the kind they need.
    /// </summary>
    private async Task<Result> VerifyInternalAsync(
        string userId,
        ChallengeVerifyRequest request,
        Omni2FaAuditEventType successEvent,
        Omni2FaAuditEventType failEvent,
        CancellationToken cancellationToken) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result.Failure(Omni2FaErrorCodes.MethodNotFound);
        }

        if (method.Type == TwoFactorMethodType.WebAuthn) {
            return await VerifyWebAuthnAsync(userId, method, request, successEvent, failEvent, cancellationToken).ConfigureAwait(false);
        }

        var verified = method.Type switch {
            TwoFactorMethodType.Totp => VerifyTotp(method, request.Code),
            TwoFactorMethodType.Email => await VerifyEmailAsync(userId, method, request.Code, cancellationToken).ConfigureAwait(false),
            _ => false,
        };
        if (!verified) {
            await AuditAsync(failEvent, userId, method, cancellationToken).ConfigureAwait(false);
            return Result.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        await _methods.MarkUsedAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await AuditAsync(successEvent, userId, method, cancellationToken).ConfigureAwait(false);

        return Result.Success();
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
        await _challenges.AddAndSaveAsync(challenge, cancellationToken).ConfigureAwait(false);

        return Result<ChallengeStartResponse>.Success(EmailResponse(challenge));
    }

    private async Task<Result<ChallengeStartResponse>> StartWebAuthnAsync(string userId, TwoFactorMethod method, CancellationToken cancellationToken) {
        if (method.WebAuthnCredentialId is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.ValidationFailed, "The WebAuthn method has no stored credential.");
        }

        var options = _webAuthn.CreateAssertionOptions(new[] { method.WebAuthnCredentialId });

        var now = DateTime.UtcNow;
        var challenge = new TwoFactorChallenge {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = TwoFactorChallengeKind.Login,
            MethodId = method.Id,
            WebAuthnChallenge = Encoding.UTF8.GetBytes(options.OptionsJson),
            CreatedAt = now,
            ExpiresAt = now.Add(_webAuthnTtl),
        };
        await _challenges.AddAndSaveAsync(challenge, cancellationToken).ConfigureAwait(false);

        return Result<ChallengeStartResponse>.Success(new ChallengeStartResponse {
            Type = TwoFactorMethodType.WebAuthn,
            OptionsJson = options.OptionsJson,
        });
    }

    private bool VerifyTotp(TwoFactorMethod method, string? code) {
        if (method.TotpSecret is null || code is null) {
            return false;
        }
        var secret = _protector.Unprotect(method.TotpSecret);
        return _totp.ValidateCode(secret, code);
    }

    private async Task<bool> VerifyEmailAsync(string userId, TwoFactorMethod method, string? code, CancellationToken cancellationToken) {
        if (code is null) {
            return false;
        }
        var challenge = await _challenges.GetActiveLoginChallengeAsync(userId, method.Id, cancellationToken).ConfigureAwait(false);
        if (challenge is null) {
            return false;
        }
        if (!_emailOtp.Verify(challenge, code)) {
            await _challenges.RecordFailedAttemptAsync(challenge, cancellationToken).ConfigureAwait(false);
            return false;
        }
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<Result> VerifyWebAuthnAsync(
        string userId,
        TwoFactorMethod method,
        ChallengeVerifyRequest request,
        Omni2FaAuditEventType successEvent,
        Omni2FaAuditEventType failEvent,
        CancellationToken cancellationToken) {
        if (request.AssertionResponseJson is null || method.WebAuthnCredentialId is null || method.WebAuthnPublicKey is null) {
            return Result.Failure(Omni2FaErrorCodes.WebAuthnVerificationFailed);
        }
        var challenge = await _challenges.GetActiveLoginChallengeAsync(userId, method.Id, cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.WebAuthnChallenge is null) {
            return Result.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }

        var optionsJson = Encoding.UTF8.GetString(challenge.WebAuthnChallenge);
        var stored = new WebAuthnStoredCredential {
            CredentialId = method.WebAuthnCredentialId,
            PublicKey = method.WebAuthnPublicKey,
            SignCount = method.WebAuthnSignCount ?? 0,
        };
        var result = await _webAuthn.VerifyAssertionAsync(optionsJson, request.AssertionResponseJson, stored, cancellationToken).ConfigureAwait(false);
        if (result is null) {
            await _challenges.RecordFailedAttemptAsync(challenge, cancellationToken).ConfigureAwait(false);
            await AuditAsync(failEvent, userId, method, cancellationToken).ConfigureAwait(false);
            return Result.Failure(Omni2FaErrorCodes.WebAuthnVerificationFailed);
        }

        method.WebAuthnSignCount = result.SignCount;
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _methods.MarkUsedAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await AuditAsync(successEvent, userId, method, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private Task AuditAsync(Omni2FaAuditEventType type, string userId, TwoFactorMethod method, CancellationToken cancellationToken) {
        return _audit.RecordAsync(new Omni2FaAuditEvent {
            Type = type,
            UserId = userId,
            MethodType = method.Type,
            MethodId = method.Id,
        }, cancellationToken);
    }

    private ChallengeStartResponse EmailResponse(TwoFactorChallenge challenge) {
        return new ChallengeStartResponse {
            Type = TwoFactorMethodType.Email,
            ExpiresAt = challenge.ExpiresAt,
            ResendAvailableAt = _emailOtp.ResendAvailableAt(challenge),
        };
    }
}
