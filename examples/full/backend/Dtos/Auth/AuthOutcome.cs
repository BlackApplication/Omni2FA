namespace Example.Backend.Dtos.Auth;

/// <summary>Discriminator on the <c>/auth/login</c> response — frontend switches on <see cref="Kind"/>.</summary>
public class AuthOutcome {
    public required AuthOutcomeKind Kind { get; init; }
    public LoginResponse? Session { get; init; }
    public TwoFactorChallengeResponse? Challenge { get; init; }
}

public enum AuthOutcomeKind {
    Session = 0,
    Challenge = 1,
}
