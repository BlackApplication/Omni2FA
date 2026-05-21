namespace Example.Backend.Configuration;

/// <summary>JWT settings for the host's own session tokens — distinct from Omni2FA pre-auth tokens.</summary>
public class HostJwtOptions {
    public const string SectionName = "HostJwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ExampleHost";
    public string Audience { get; set; } = "ExampleHost-Session";
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(8);
}
