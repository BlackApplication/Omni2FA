namespace Example.Backend.Services.Interfaces;

/// <summary>Hashes and verifies passwords. Production hosts replace this with their preferred algorithm (BCrypt/Argon2id).</summary>
public interface IPasswordHasher {
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}
