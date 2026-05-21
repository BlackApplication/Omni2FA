using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Omni2FA.AspNetCore.EntityFrameworkCore.Options;
using Omni2FA.Core.Entities;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Configurations;

/// <summary>EF Core mapping for <see cref="TwoFactorChallenge"/>.</summary>
public class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge> {
    private readonly EfMappingOptions _options;

    public TwoFactorChallengeConfiguration(EfMappingOptions options) {
        _options = options;
    }

    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder) {
        if (string.IsNullOrEmpty(_options.Schema)) {
            builder.ToTable(_options.ChallengesTableName);
        } else {
            builder.ToTable(_options.ChallengesTableName, _options.Schema);
        }

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Kind).HasConversion<int>().IsRequired();
        builder.Property(c => c.TotpSecretCandidate).HasMaxLength(512);
        builder.Property(c => c.EmailOtpHash).HasMaxLength(256);
        builder.Property(c => c.WebAuthnChallenge).HasMaxLength(256);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.ExpiresAt).IsRequired();
        builder.Property(c => c.FailedAttempts).IsRequired();

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName($"IX_{_options.ChallengesTableName}_UserId");

        builder.HasIndex(c => c.ExpiresAt)
            .HasDatabaseName($"IX_{_options.ChallengesTableName}_ExpiresAt");
    }
}
