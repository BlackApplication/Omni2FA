using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Dtos;

/// <summary>Public-facing shape of an enrolled 2FA method.</summary>
public class TwoFactorMethodDto {
    public required Guid Id { get; init; }

    public required TwoFactorMethodType Type { get; init; }

    public string? Name { get; init; }

    /// <summary>UTC.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>UTC. Null if never used.</summary>
    public DateTime? LastUsedAt { get; init; }
}
