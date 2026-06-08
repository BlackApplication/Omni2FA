namespace Omni2FA.Core.Configuration;

/// <summary>Recovery-code settings.</summary>
public class RecoveryCodeOptions {
    /// <summary>How many codes to generate per set (on first enrollment and on regeneration).</summary>
    public int Count { get; set; } = 10;
}
