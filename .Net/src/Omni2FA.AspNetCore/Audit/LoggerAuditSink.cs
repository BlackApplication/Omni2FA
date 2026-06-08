using Microsoft.Extensions.Logging;
using Omni2FA.Core.Audit;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Audit;

/// <summary>Default <see cref="IOmni2FaAuditSink"/> — writes a structured <c>ILogger</c> record per event. Registered via <c>TryAdd</c> so a host audit sink replaces it.</summary>
public sealed partial class LoggerAuditSink : IOmni2FaAuditSink {
    private readonly ILogger<LoggerAuditSink> _logger;

    public LoggerAuditSink(ILogger<LoggerAuditSink> logger) {
        _logger = logger;
    }

    public Task RecordAsync(Omni2FaAuditEvent auditEvent, CancellationToken cancellationToken = default) {
        LogAudit(auditEvent.Type, auditEvent.UserId, auditEvent.MethodType, auditEvent.MethodId, auditEvent.Detail);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Omni2FA audit {EventType}: user={UserId} methodType={MethodType} methodId={MethodId} detail={Detail}")]
    private partial void LogAudit(Omni2FaAuditEventType eventType, string? userId, TwoFactorMethodType? methodType, Guid? methodId, string? detail);
}
