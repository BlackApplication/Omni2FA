using Microsoft.Extensions.Options;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TwoFactorMethodService : ITwoFactorMethodService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly IRecoveryCodeService _recovery;
    private readonly IOmni2FaAuditSink _audit;
    private readonly bool _allowDisablingLastMethod;

    public TwoFactorMethodService(
        ITwoFactorMethodStore methods,
        IRecoveryCodeService recovery,
        IOmni2FaAuditSink audit,
        IOptions<Omni2FaOptions> options) {
        _methods = methods;
        _recovery = recovery;
        _audit = audit;
        _allowDisablingLastMethod = options.Value.AspNetCore.AllowDisablingLastMethod;
    }

    public async Task<IReadOnlyList<TwoFactorMethodDto>> ListAsync(string userId, CancellationToken cancellationToken = default) {
        var methods = await _methods.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var result = new List<TwoFactorMethodDto>(methods.Count);
        foreach (var m in methods) {
            result.Add(new TwoFactorMethodDto {
                Id = m.Id,
                Type = m.Type,
                Name = m.Name,
                CreatedAt = m.CreatedAt,
                LastUsedAt = m.LastUsedAt,
            });
        }
        return result;
    }

    public async Task<Result> RemoveAsync(string userId, Guid methodId, CancellationToken cancellationToken = default) {
        var method = await _methods.GetActiveAsync(methodId, userId, cancellationToken).ConfigureAwait(false);
        if (method is null) {
            return Result.Failure(Omni2FaErrorCodes.MethodNotFound);
        }

        var active = await _methods.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var isLast = active.Count <= 1;
        if (isLast && !_allowDisablingLastMethod) {
            return Result.Failure(Omni2FaErrorCodes.LastMethodProtected);
        }

        await _methods.RemoveAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (isLast) {
            await _recovery.WipeAsync(userId, cancellationToken).ConfigureAwait(false);
        }
        await _audit.RecordAsync(new Omni2FaAuditEvent {
            Type = Omni2FaAuditEventType.MethodRemoved,
            UserId = userId,
            MethodType = method.Type,
            MethodId = method.Id,
        }, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
