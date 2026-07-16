using Flowist.Shared.Enums;
using Flowist.TaskService.Data;
using Flowist.TaskService.Entities;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Flowist.TaskService.Tests.TestSupport;

internal static class TaskServiceTestFactory
{
    internal static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    internal static readonly Guid MemberId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    internal static readonly Guid OutsiderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    internal static TaskServiceDbContext CreateDbContext()
    {
        DbContextOptions<TaskServiceDbContext> options = new DbContextOptionsBuilder<TaskServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskServiceDbContext(options);
    }

    internal static IPublishEndpoint CreatePublishEndpoint()
    {
        Mock<IPublishEndpoint> publishEndpoint = new();
        return publishEndpoint.Object;
    }

    internal static NullLogger<T> CreateLogger<T>() => NullLogger<T>.Instance;

    internal static Workspace AddWorkspace(
        TaskServiceDbContext dbContext,
        Guid? workspaceId = null,
        Guid? ownerId = null,
        string name = "Workspace")
    {
        Guid effectiveWorkspaceId = workspaceId ?? Guid.NewGuid();
        Guid effectiveOwnerId = ownerId ?? OwnerId;

        Workspace workspace = new()
        {
            Id = effectiveWorkspaceId,
            Name = name,
            Description = "Workspace description",
            OwnerId = effectiveOwnerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Workspaces.Add(workspace);
        AddWorkspaceMember(dbContext, effectiveWorkspaceId, effectiveOwnerId, WorkspaceRole.Owner);
        dbContext.SaveChanges();

        return workspace;
    }

    internal static WorkspaceMember AddWorkspaceMember(
        TaskServiceDbContext dbContext,
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role)
    {
        WorkspaceMember member = new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTimeOffset.UtcNow
        };

        dbContext.WorkspaceMembers.Add(member);
        return member;
    }

    internal static Project AddProject(
        TaskServiceDbContext dbContext,
        Guid workspaceId,
        Guid? projectId = null,
        string name = "Project")
    {
        Project project = new()
        {
            Id = projectId ?? Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Description = "Project description",
            CreatedBy = OwnerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Projects.Add(project);
        dbContext.SaveChanges();

        return project;
    }

    internal static TaskItem AddTask(
        TaskServiceDbContext dbContext,
        Guid projectId,
        Guid? taskId = null,
        string title = "Task")
    {
        TaskItem task = new()
        {
            Id = taskId ?? Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = "Task description",
            Status = Flowist.Shared.Enums.TaskStatus.Todo,
            Priority = TaskPriority.Medium,
            CreatedBy = OwnerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.TaskItems.Add(task);
        dbContext.SaveChanges();

        return task;
    }
}