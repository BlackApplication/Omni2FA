using Omni2FA.Core.Dtos;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TwoFactorChallengeService : ITwoFactorChallengeService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITotpService _totp;
    private readonly ISecretProtector _protector;

    public TwoFactorChallengeService(
        ITwoFactorMethodStore methods,
        ITotpService totp,
        ISecretProtector protector) {
        _methods = methods;
        _totp = totp;
        _protector = protector;
    }

    public async Task<Result<ChallengeStartResponse>> StartAsync(string userId, ChallengeStartRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<ChallengeStartResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }
        return Result<ChallengeStartResponse>.Success(new ChallengeStartResponse {
            Type = method.Type,
        });
    }

    public async Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, ChallengeVerifyRequest request, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(request.MethodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.MethodNotFound);
        }
        if (method.Type != TwoFactorMethodType.Totp || method.TotpSecret is null) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        var secret = _protector.Unprotect(method.TotpSecret);
        if (!_totp.ValidateCode(secret, request.Code)) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.InvalidCode);
        }

        await _methods.MarkUsedAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerifySuccessResponse>.Success(new VerifySuccessResponse {
            Verified = true,
            UserId = userId,
        });
    }
}
