using Microsoft.Extensions.Options;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Helpers;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

/// <summary>Default recovery-code service — generates, regenerates, verifies, and wipes one-time codes.</summary>
public class RecoveryCodeService : IRecoveryCodeService {
    private readonly IRecoveryCodeStore _store;
    private readonly IOmni2FaAuditSink _audit;
    private readonly IPreAuthTokenIssuer _preAuth;
    private readonly int _count;

    public RecoveryCodeService(IRecoveryCodeStore store, IOmni2FaAuditSink audit, IPreAuthTokenIssuer preAuth, IOptions<Omni2FaOptions> options) {
        _store = store;
        _audit = audit;
        _preAuth = preAuth;
        _count = options.Value.RecoveryCodes.Count;
    }

    public async Task<IReadOnlyList<string>?> GenerateIfNoneAsync(string userId, CancellationToken cancellationToken = default) {
        if (await _store.HasAnyAsync(userId, cancellationToken).ConfigureAwait(false)) {
            return null;
        }
        var codes = await GenerateAndSaveAsync(userId, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent { Type = Omni2FaAuditEventType.RecoveryCodesGenerated, UserId = userId }, cancellationToken).ConfigureAwait(false);
        return codes;
    }

    public async Task<Result<RecoveryCodesResponse>> RegenerateAsync(string userId, CancellationToken cancellationToken = default) {
        await _store.DeleteByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var codes = await GenerateAndSaveAsync(userId, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent { Type = Omni2FaAuditEventType.RecoveryCodesRegenerated, UserId = userId }, cancellationToken).ConfigureAwait(false);
        return Result<RecoveryCodesResponse>.Success(new RecoveryCodesResponse { RecoveryCodes = codes });
    }

    public async Task<Result<VerifySuccessResponse>> VerifyAsync(string userId, RecoveryCodeVerifyRequest request, CancellationToken cancellationToken = default) {
        var hash = RecoveryCodeHelpers.Hash(request.RecoveryCode);
        var all = await _store.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var match = all.FirstOrDefault(c => c.CodeHash == hash);
        if (match is null) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.RecoveryCodeInvalid);
        }
        if (match.UsedAt is not null) {
            return Result<VerifySuccessResponse>.Failure(Omni2FaErrorCodes.RecoveryCodeUsed);
        }

        await _store.MarkUsedAsync(match, cancellationToken).ConfigureAwait(false);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent { Type = Omni2FaAuditEventType.RecoveryCodeUsed, UserId = userId }, cancellationToken).ConfigureAwait(false);

        // Same verified-handoff token as method verify — recovery-code login isn't a special case for the host.
        var handoff = _preAuth.IssueVerified(userId);
        return Result<VerifySuccessResponse>.Success(new VerifySuccessResponse {
            Verified = true,
            UserId = userId,
            VerifiedToken = handoff.Token,
            ExpiresAt = handoff.ExpiresAt,
        });
    }

    public Task WipeAsync(string userId, CancellationToken cancellationToken = default) {
        return _store.DeleteByUserAsync(userId, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GenerateAndSaveAsync(string userId, CancellationToken cancellationToken) {
        var now = DateTime.UtcNow;
        var plaintext = new List<string>(_count);
        var entities = new List<RecoveryCode>(_count);
        for (var i = 0; i < _count; i++) {
            var code = RecoveryCodeHelpers.GenerateCode();
            plaintext.Add(code);
            entities.Add(new RecoveryCode {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = RecoveryCodeHelpers.Hash(code),
                CreatedAt = now,
            });
        }
        await _store.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plaintext;
    }
}
