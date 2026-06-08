namespace Omni2FA.Core.Configuration;

/// <summary>
/// Rate-limit settings for the sensitive endpoints (verify, resend, recovery-code, enroll confirm).
/// Partitioned by client IP. Default: 20 attempts per minute per IP.
/// </summary>
public class RateLimitOptions {
    /// <summary>Master switch. Set false to disable Omni2FA's built-in limiter (e.g. when fronted by an external one).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Max attempts allowed per IP within <see cref="Window"/>.</summary>
    public int PermitLimit { get; set; } = 20;

    /// <summary>Length of the fixed window.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
