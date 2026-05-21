using Example.Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Omni2FA.AspNetCore.EntityFrameworkCore.Extensions;

namespace Example.Backend;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b => {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).HasMaxLength(256).IsRequired();
            b.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.ApplyOmni2FaConfiguration();
    }
}
