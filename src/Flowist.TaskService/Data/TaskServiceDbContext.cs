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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureWorkspace(modelBuilder);
        ConfigureWorkspaceMember(modelBuilder);
        ConfigureProject(modelBuilder);
        ConfigureTaskItem(modelBuilder);
    }

    private static void ConfigureTaskItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");

            entity.HasKey(task => task.Id);

            entity.Property(task => task.Title)
                .IsRequired()
                .HasMaxLength(ValidationConstants.TaskTitleMaxLength);

            entity.Property(task => task.Description)
                .HasMaxLength(ValidationConstants.TaskDescriptionMaxLength);

            entity.Property(task => task.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(task => task.Priority)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(task => task.AssigneeId);

            entity.Property(task => task.ProjectId)
                .IsRequired();

            entity.Property(task => task.CreatedBy)
                .IsRequired();

            entity.Property(task => task.DueDate);

            entity.Property(task => task.CreatedAt)
                .IsRequired();

            entity.Property(task => task.UpdatedAt);

            entity.HasIndex(task => task.ProjectId);

            entity.HasIndex(task => task.Status);

            entity.HasIndex(task => task.Priority);

            entity.HasIndex(task => task.AssigneeId);

            entity.HasIndex(task => new
            {
                task.ProjectId,
                task.Status
            });

            entity.HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");

            entity.HasKey(project => project.Id);

            entity.Property(project => project.Name)
                .IsRequired()
                .HasMaxLength(ValidationConstants.ProjectNameMaxLength);

            entity.Property(project => project.Description)
                .HasMaxLength(ValidationConstants.ProjectDescriptionMaxLength);

            entity.Property(project => project.WorkspaceId)
                .IsRequired();

            entity.Property(project => project.CreatedBy)
                .IsRequired();

            entity.Property(project => project.CreatedAt)
                .IsRequired();

            entity.Property(project => project.UpdatedAt);

            entity.HasIndex(project => project.WorkspaceId);

            entity.HasIndex(project => new
            {
                project.WorkspaceId,
                project.Name
            }).IsUnique();

            entity.HasOne(project => project.Workspace)
                .WithMany(workspace => workspace.Projects)
                .HasForeignKey(project => project.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}