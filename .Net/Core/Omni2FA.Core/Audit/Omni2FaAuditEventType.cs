namespace Omni2FA.Core.Audit;

/// <summary>Kinds of security-relevant events Omni2FA raises through <see cref="Services.Interfaces.IOmni2FaAuditSink"/>.</summary>
public enum Omni2FaAuditEventType {
    MethodEnrolled,
    MethodRemoved,
    LoginVerifySucceeded,
    LoginVerifyFailed,
    RecoveryCodesGenerated,
    RecoveryCodeUsed,
    RecoveryCodesRegenerated,
    RateLimitExceeded,
}
