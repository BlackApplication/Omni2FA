using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Audit;

/// <summary>A security-relevant event for the host's audit pipeline. Carries only non-sensitive identifiers — never codes or secrets.</summary>
public class Omni2FaAuditEvent {
    public required Omni2FaAuditEventType Type { get; init; }

    /// <summary>Affected user, when known.</summary>
    public string? UserId { get; init; }

    /// <summary>Method type involved, when applicable.</summary>
    public TwoFactorMethodType? MethodType { get; init; }

    /// <summary>Method id involved, when applicable.</summary>
    public Guid? MethodId { get; init; }

    /// <summary>Optional short, non-sensitive context (e.g. error code, client IP).</summary>
    public string? Detail { get; init; }
}
