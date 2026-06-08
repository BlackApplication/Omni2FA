using Omni2FA.Core.Audit;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>Default <see cref="IEnrollmentFinalizer"/> — generates first-method recovery codes and audits the enrollment.</summary>
public class EnrollmentFinalizer : IEnrollmentFinalizer {
    private readonly IRecoveryCodeService _recovery;
    private readonly IOmni2FaAuditSink _audit;

    public EnrollmentFinalizer(IRecoveryCodeService recovery, IOmni2FaAuditSink audit) {
        _recovery = recovery;
        _audit = audit;
    }

    public async Task<MethodCreatedResponse> FinalizeAsync(string userId, TwoFactorMethod method, CancellationToken cancellationToken = default) {
        var recoveryCodes = await _recovery.GenerateIfNoneAsync(userId, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(new Omni2FaAuditEvent {
            Type = Omni2FaAuditEventType.MethodEnrolled,
            UserId = userId,
            MethodType = method.Type,
            MethodId = method.Id,
        }, cancellationToken).ConfigureAwait(false);

        return new MethodCreatedResponse {
            MethodId = method.Id,
            RecoveryCodes = recoveryCodes,
        };
    }
}
