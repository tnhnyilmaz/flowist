using Flowist.ActivityService.Entities;

using Microsoft.EntityFrameworkCore;

namespace Flowist.ActivityService.Data;

public sealed class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureActivityLog(modelBuilder);
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
    private static void ConfigureActivityLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");

            entity.HasKey(activityLog => activityLog.Id);

            entity.Property(activityLog => activityLog.WorkspaceId);

            entity.Property(activityLog => activityLog.UserId)
                .IsRequired();

            entity.Property(activityLog => activityLog.ActionType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(activityLog => activityLog.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(activityLog => activityLog.EntityId)
                .IsRequired();

            entity.Property(activityLog => activityLog.Description)
                .IsRequired()
                .HasMaxLength(1_000);

            entity.Property(activityLog => activityLog.Metadata)
                .HasColumnType("jsonb");

            entity.Property(activityLog => activityLog.CreatedAt)
                .IsRequired();

            entity.HasIndex(activityLog => activityLog.WorkspaceId);

            entity.HasIndex(activityLog => activityLog.UserId);

            entity.HasIndex(activityLog => activityLog.ActionType);

            entity.HasIndex(activityLog => activityLog.CreatedAt);

            entity.HasIndex(activityLog => new
            {
                activityLog.WorkspaceId,
                activityLog.CreatedAt
            });
        });
    }
}