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

        builder.Property(m => m.UserId).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Type).HasConversion<int>().IsRequired();
        builder.Property(m => m.Name).HasMaxLength(128);
        builder.Property(m => m.IsActive).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.TotpSecret).HasMaxLength(512);
        builder.Property(m => m.EmailAddress).HasMaxLength(256);
        builder.Property(m => m.WebAuthnCredentialId).HasMaxLength(256);
        // No length cap — COSE public keys for RSA authenticators can exceed 512 bytes.

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName($"IX_{_options.MethodsTableName}_UserId");

        builder.HasIndex(m => new { m.UserId, m.Type })
            .HasDatabaseName($"IX_{_options.MethodsTableName}_UserId_Type");

        // WebAuthn credential ids are globally unique. The column is nullable (TOTP/Email rows have
        // none); EF filters the unique index to non-null values on providers that need it.
        builder.HasIndex(m => m.WebAuthnCredentialId)
            .IsUnique()
            .HasDatabaseName($"IX_{_options.MethodsTableName}_WebAuthnCredentialId");
    }
}
