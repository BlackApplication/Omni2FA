using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Dtos;

/// <summary>Public-facing shape of an enrolled 2FA method.</summary>
public class TwoFactorMethodDto {
    /// <summary>Identifier of the enrolled method.</summary>
    public required Guid Id { get; init; }

    /// <summary>Kind of factor.</summary>
    public required TwoFactorMethodKind Kind { get; init; }

    /// <summary>Optional human-readable label.</summary>
    public string? Name { get; init; }

    /// <summary>When the method was enrolled (UTC).</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>When the method was last successfully used (UTC). Null if never used.</summary>
    public DateTime? LastUsedAt { get; init; }
}
