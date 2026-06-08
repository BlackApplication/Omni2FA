using Omni2FA.Core.Dtos;

namespace Example.Backend.Dtos.Auth;

/// <summary>Discriminator on the <c>/auth/login</c> response — frontend switches on <see cref="Kind"/>.</summary>
public class AuthOutcome {
    public required AuthOutcomeKind Kind { get; init; }
    public LoginResponse? Session { get; init; }

    // Reuse the library's PreAuthChallengeResponse (PreAuthToken + AvailableMethods + ExpiresAt) — no need for a host DTO.
    public PreAuthChallengeResponse? Challenge { get; init; }
}

public enum AuthOutcomeKind {
    Session = 0,
    Challenge = 1,
}
