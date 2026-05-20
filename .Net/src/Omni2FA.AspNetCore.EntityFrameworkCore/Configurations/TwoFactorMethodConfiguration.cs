using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Omni2FA.AspNetCore.EntityFrameworkCore.Options;
using Omni2FA.Core.Entities;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Configurations;

/// <summary>EF Core mapping for <see cref="TwoFactorMethod"/>.</summary>
public class TwoFactorMethodConfiguration : IEntityTypeConfiguration<TwoFactorMethod> {
    private readonly EfMappingOptions _options;

    public TwoFactorMethodConfiguration(EfMappingOptions options) {
        _options = options;
    }

    public void Configure(EntityTypeBuilder<TwoFactorMethod> builder) {
        if (string.IsNullOrEmpty(_options.Schema)) {
            builder.ToTable(_options.MethodsTableName);
        } else {
            builder.ToTable(_options.MethodsTableName, _options.Schema);
        }

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Kind).HasConversion<int>().IsRequired();
        builder.Property(m => m.Name).HasMaxLength(128);
        builder.Property(m => m.IsActive).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.TotpSecret).HasMaxLength(512);
        builder.Property(m => m.WebAuthnCredentialId).HasMaxLength(256);
        builder.Property(m => m.WebAuthnPublicKey).HasMaxLength(512);

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName($"IX_{_options.MethodsTableName}_UserId");

        builder.HasIndex(m => new { m.UserId, m.Kind })
            .HasDatabaseName($"IX_{_options.MethodsTableName}_UserId_Kind");
    }
}
