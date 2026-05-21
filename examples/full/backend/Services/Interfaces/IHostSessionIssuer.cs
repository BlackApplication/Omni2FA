namespace Example.Backend.Services.Interfaces;

/// <summary>Mints the host's final session JWT after either (a) password-only login when 2FA isn't enrolled, or (b) successful 2FA verification.</summary>
public interface IHostSessionIssuer {
    string Issue(Guid userId, string email);
}
