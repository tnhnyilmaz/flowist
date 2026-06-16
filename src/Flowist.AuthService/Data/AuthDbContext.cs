using Flowist.AuthService.Entities;
using Flowist.Shared.Constants;

using Microsoft.EntityFrameworkCore;

namespace Flowist.AuthService.Data;

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(ValidationConstants.EmailMaxLength);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(user => user.FullName)
                .IsRequired()
                .HasMaxLength(ValidationConstants.FullNameMaxLength);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.Property(user => user.UpdatedAt);
            
            entity.Property(user => user.FailedLoginAttempts)
                .IsRequired();

            entity.Property(user => user.LockedUntil);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(refreshToken => refreshToken.Id);

            entity.Property(refreshToken => refreshToken.Token)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(refreshToken => refreshToken.IpAddress)
                .HasMaxLength(100);
            entity.HasIndex(refreshToken => refreshToken.Token)
                .IsUnique();

            entity.Property(refreshToken => refreshToken.ExpiresAt)
                .IsRequired();

            entity.Property(refreshToken => refreshToken.CreatedAt)
                .IsRequired();

            entity.Property(refreshToken => refreshToken.RevokedAt);

            entity.Property(refreshToken => refreshToken.DeviceInfo)
                .HasMaxLength(500);

            entity.Ignore(refreshToken => refreshToken.IsExpired);
            entity.Ignore(refreshToken => refreshToken.IsRevoked);
            entity.Ignore(refreshToken => refreshToken.IsActive);

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}