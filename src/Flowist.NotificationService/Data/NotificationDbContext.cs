using Flowist.NotificationService.Entities;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Data;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureNotification(modelBuilder);
        ConfigureProcessedEvent(modelBuilder);
    }
    private static void ConfigureProcessedEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");

            entity.HasKey(processedEvent => processedEvent.EventId);

            entity.Property(processedEvent => processedEvent.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(processedEvent => processedEvent.ProcessedAt)
                .IsRequired();

            entity.HasIndex(processedEvent => processedEvent.EventType);
        });
    }
    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.UserId)
                .IsRequired();

            entity.Property(notification => notification.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(notification => notification.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(notification => notification.Message)
                .IsRequired()
                .HasMaxLength(1_000);

            entity.Property(notification => notification.IsRead)
                .IsRequired();

            entity.Property(notification => notification.CreatedAt)
                .IsRequired();

            entity.Property(notification => notification.ReadAt);

            entity.HasIndex(notification => notification.UserId);

            entity.HasIndex(notification => new
            {
                notification.UserId,
                notification.IsRead
            });

            entity.HasIndex(notification => notification.CreatedAt);
        });
    }
}