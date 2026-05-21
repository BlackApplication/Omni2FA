namespace Example.Backend.Entities;

/// <summary>Host's user entity. Owned by the host application — Omni2FA only stores a stringified user id reference.</summary>
public class User {
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash. Never stored or transmitted in plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
