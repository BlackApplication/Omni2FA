namespace Omni2FA.Core.Dtos;

/// <summary>
/// Uniform error envelope returned by every non-2xx Omni2FA response. Frontends switch on
/// <see cref="Code"/> (stable, machine-readable); <see cref="Message"/> is informational
/// and may be localized.
/// </summary>
public class ErrorResponse {
    /// <summary>Stable machine-readable error code.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable message. May be localized by the host.</summary>
    public required string Message { get; init; }

    /// <summary>Optional structured context. Shape depends on <see cref="Code"/>.</summary>
    public IDictionary<string, object?>? Details { get; init; }
}
