using Flowist.Shared.Constants;
using Flowist.TaskService.Entities;

using Microsoft.EntityFrameworkCore;

namespace Flowist.TaskService.Data;

public sealed class TaskServiceDbContext : DbContext
{
    public TaskServiceDbContext(DbContextOptions<TaskServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureWorkspace(modelBuilder);
        ConfigureWorkspaceMember(modelBuilder);
    }

    private static void ConfigureWorkspace(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("Workspaces");

            entity.HasKey(workspace => workspace.Id);

            entity.Property(workspace => workspace.Name)
                .IsRequired()
                .HasMaxLength(ValidationConstants.WorkspaceNameMaxLength);

            entity.Property(workspace => workspace.Description)
                .HasMaxLength(ValidationConstants.WorkspaceDescriptionMaxLength);

            entity.Property(workspace => workspace.OwnerId)
                .IsRequired();

            entity.Property(workspace => workspace.CreatedAt)
                .IsRequired();

            entity.Property(workspace => workspace.UpdatedAt);

            entity.HasIndex(workspace => workspace.OwnerId);

            entity.HasIndex(workspace => new
            {
                workspace.OwnerId,
                workspace.Name
            });
        });
    }

    private static void ConfigureWorkspaceMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("WorkspaceMembers");

            entity.HasKey(member => member.Id);

            entity.Property(member => member.WorkspaceId)
                .IsRequired();

            entity.Property(member => member.UserId)
                .IsRequired();

            entity.Property(member => member.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(member => member.JoinedAt)
                .IsRequired();

            entity.HasIndex(member => member.UserId);

            entity.HasIndex(member => new
            {
                member.WorkspaceId,
                member.UserId
            }).IsUnique();

            entity.HasOne(member => member.Workspace)
                .WithMany(workspace => workspace.Members)
                .HasForeignKey(member => member.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}