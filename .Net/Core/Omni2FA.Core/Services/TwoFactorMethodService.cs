using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

public class TwoFactorMethodService : ITwoFactorMethodService {
    private readonly ITwoFactorMethodStore _methods;

    public TwoFactorMethodService(ITwoFactorMethodStore methods) {
        _methods = methods;
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
        await _methods.RemoveAsync(method, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
