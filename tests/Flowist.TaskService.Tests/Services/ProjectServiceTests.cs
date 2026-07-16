using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Services;
using Flowist.TaskService.Tests.TestSupport;

using FluentAssertions;

namespace Flowist.TaskService.Tests.Services;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateProject_WhenCurrentUserIsOwner()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        ProjectService service = CreateService(dbContext);

        ProjectDto result = await service.CreateAsync(
            workspace.Id,
            new CreateProjectRequest("  Project A  ", "  Description  "),
            TaskServiceTestFactory.OwnerId);

        result.Name.Should().Be("Project A");
        result.WorkspaceId.Should().Be(workspace.Id);
        dbContext.Projects.Should().ContainSingle(project => project.Id == result.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowForbiddenAccessException_WhenCurrentUserIsNotWorkspaceMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        ProjectService service = CreateService(dbContext);

        Func<Task> act = () => service.CreateAsync(
            workspace.Id,
            new CreateProjectRequest("Project", null),
            TaskServiceTestFactory.OutsiderId);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenProjectNameAlreadyExistsInWorkspace()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddProject(dbContext, workspace.Id, name: "Project");
        ProjectService service = CreateService(dbContext);

        Func<Task> act = () => service.CreateAsync(
            workspace.Id,
            new CreateProjectRequest("Project", null),
            TaskServiceTestFactory.OwnerId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetWorkspaceProjectsAsync_ShouldReturnProjects_WhenCurrentUserIsMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id, name: "Project");
        ProjectService service = CreateService(dbContext);

        IReadOnlyCollection<ProjectDto> result = await service.GetWorkspaceProjectsAsync(workspace.Id, TaskServiceTestFactory.MemberId);

        result.Should().ContainSingle(item => item.Id == project.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistProjectChanges_WhenCurrentUserIsAdmin()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.AdminId, WorkspaceRole.Admin);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id, name: "Old");
        ProjectService service = CreateService(dbContext);

        ProjectDto result = await service.UpdateAsync(
            project.Id,
            new UpdateProjectRequest("  New  ", "  New description  "),
            TaskServiceTestFactory.AdminId);

        result.Name.Should().Be("New");
        dbContext.Projects.Single(entity => entity.Id == project.Id).Name.Should().Be("New");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProject_WhenCurrentUserIsOwner()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        var project = TaskServiceTestFactory.AddProject(dbContext, workspace.Id);
        ProjectService service = CreateService(dbContext);

        await service.DeleteAsync(project.Id, TaskServiceTestFactory.OwnerId);

        dbContext.Projects.Should().NotContain(entity => entity.Id == project.Id);
    }

    private static ProjectService CreateService(Flowist.TaskService.Data.TaskServiceDbContext dbContext)
    {
        return new ProjectService(
            dbContext,
            TaskServiceTestFactory.CreatePublishEndpoint(),
            TaskServiceTestFactory.CreateLogger<ProjectService>());
    }
}