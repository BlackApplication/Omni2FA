using Omni2FA.Core.Audit;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Sink for Omni2FA audit events. Opt-in: the adapter registers a default <c>ILogger</c> implementation
/// via <c>TryAdd</c>, so a host that registers its own (database, SIEM, queue) replaces it, and one that
/// does nothing extra still gets structured log records — never a null reference.
/// </summary>
public interface IOmni2FaAuditSink {
    /// <summary>Record an event. Implementations must not throw into the caller — auditing is best-effort.</summary>
    Task RecordAsync(Omni2FaAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
