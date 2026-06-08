namespace Omni2FA.Core.Entities;

/// <summary>
/// A single one-time backup code. Only the hash is stored; the plaintext is shown to the user once
/// at generation. Consumed by stamping <see cref="UsedAt"/>.
/// </summary>
public class RecoveryCode {
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 hash (lower-case hex) of the normalized code.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC. Null = unused.</summary>
    public DateTime? UsedAt { get; set; }
}
