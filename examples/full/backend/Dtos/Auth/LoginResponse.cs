namespace Example.Backend.Dtos.Auth;

/// <summary>Final session — host's own JWT after either password-only login or successful 2FA verification.</summary>
public class LoginResponse {
    public required string SessionToken { get; init; }
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
