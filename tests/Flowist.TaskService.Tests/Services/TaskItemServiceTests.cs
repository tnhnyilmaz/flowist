using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Services;
using Flowist.TaskService.Tests.TestSupport;

using FluentAssertions;

namespace Flowist.TaskService.Tests.Services;

public sealed class TaskItemServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateTask_WhenCurrentUserIsWorkspaceMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        TaskItemService service = CreateService(dbContext);

        TaskItemDto result = await service.CreateAsync(
            project.Id,
            new CreateTaskRequest("  Task A  ", "  Description  ", TaskPriority.High, null, null),
            TaskServiceTestFactory.MemberId);

        result.Title.Should().Be("Task A");
        result.Status.Should().Be(Flowist.Shared.Enums.TaskStatus.Todo);
        result.ProjectId.Should().Be(project.Id);
        dbContext.TaskItems.Should().ContainSingle(task => task.Id == result.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenAssigneeIsNotWorkspaceMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        TaskItemService service = CreateService(dbContext);

        Func<Task> act = () => service.CreateAsync(
            project.Id,
            new CreateTaskRequest("Task", null, TaskPriority.Medium, TaskServiceTestFactory.OutsiderId, null),
            TaskServiceTestFactory.OwnerId);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignTask_WhenCurrentUserIsAdmin()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.AdminId, WorkspaceRole.Admin);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        var task = TaskServiceTestFactory.AddTask(dbContext, project.Id);
        TaskItemService service = CreateService(dbContext);

        TaskItemDto result = await service.AssignAsync(
            task.Id,
            new AssignTaskRequest(TaskServiceTestFactory.MemberId),
            TaskServiceTestFactory.AdminId);

        result.AssigneeId.Should().Be(TaskServiceTestFactory.MemberId);
        dbContext.TaskItems.Single(entity => entity.Id == task.Id).AssigneeId.Should().Be(TaskServiceTestFactory.MemberId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowForbiddenAccessException_WhenCurrentUserIsOnlyMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        var task = TaskServiceTestFactory.AddTask(dbContext, project.Id);
        TaskItemService service = CreateService(dbContext);

        Func<Task> act = () => service.DeleteAsync(task.Id, TaskServiceTestFactory.MemberId);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPersistStatus_WhenCurrentUserIsMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        var task = TaskServiceTestFactory.AddTask(dbContext, project.Id);
        TaskItemService service = CreateService(dbContext);

        TaskItemDto result = await service.UpdateStatusAsync(
            task.Id,
            new UpdateTaskStatusRequest(Flowist.Shared.Enums.TaskStatus.Done),
            TaskServiceTestFactory.MemberId);

        result.Status.Should().Be(Flowist.Shared.Enums.TaskStatus.Done);
        dbContext.TaskItems.Single(entity => entity.Id == task.Id).Status.Should().Be(Flowist.Shared.Enums.TaskStatus.Done);
    }

    [Fact]
    public async Task GetProjectTasksAsync_ShouldApplyStatusFilterAndPaging()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        TaskServiceTestFactory.AddTask(dbContext, project.Id, title: "Todo task");
        var doneTask = TaskServiceTestFactory.AddTask(dbContext, project.Id, title: "Done task");
        doneTask.Status = Flowist.Shared.Enums.TaskStatus.Done;
        await dbContext.SaveChangesAsync();
        TaskItemService service = CreateService(dbContext);

        PagedResult<TaskItemDto> result = await service.GetProjectTasksAsync(
            project.Id,
            new TaskFilterRequest(Flowist.Shared.Enums.TaskStatus.Done, null, null, null, null, null, false, 1, 10),
            TaskServiceTestFactory.OwnerId);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.Title == "Done task");
    }

    private static TaskItemService CreateService(Flowist.TaskService.Data.TaskServiceDbContext dbContext)
    {
        return new TaskItemService(
            dbContext,
            TaskServiceTestFactory.CreatePublishEndpoint(),
            TaskServiceTestFactory.CreateLogger<TaskItemService>());
    }
}