using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Services;
using Flowist.TaskService.Tests.TestSupport;

using FluentAssertions;

namespace Flowist.TaskService.Tests.Services;

public sealed class WorkspaceServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateWorkspaceAndOwnerMember()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        WorkspaceService service = CreateService(dbContext);

        WorkspaceDto result = await service.CreateAsync(
            new CreateWorkspaceRequest("  Team Workspace  ", "Description"),
            TaskServiceTestFactory.OwnerId);

        result.Name.Should().Be("Team Workspace");
        result.OwnerId.Should().Be(TaskServiceTestFactory.OwnerId);

        dbContext.Workspaces.Should().ContainSingle(workspace => workspace.Id == result.Id);
        dbContext.WorkspaceMembers.Should().ContainSingle(member =>
            member.WorkspaceId == result.Id &&
            member.UserId == TaskServiceTestFactory.OwnerId &&
            member.Role == WorkspaceRole.Owner);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenOwnerAlreadyHasWorkspaceWithSameName()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        TaskServiceTestFactory.AddWorkspace(dbContext, ownerId: TaskServiceTestFactory.OwnerId, name: "Existing");
        WorkspaceService service = CreateService(dbContext);

        Func<Task> act = () => service.CreateAsync(
            new CreateWorkspaceRequest("Existing", null),
            TaskServiceTestFactory.OwnerId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ShouldReturnOnlyCurrentUserMemberships()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var ownedWorkspace = TaskServiceTestFactory.AddWorkspace(dbContext, ownerId: TaskServiceTestFactory.OwnerId, name: "Owned");
        var otherWorkspace = TaskServiceTestFactory.AddWorkspace(dbContext, ownerId: TaskServiceTestFactory.AdminId, name: "Other");
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, otherWorkspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        await dbContext.SaveChangesAsync();
        WorkspaceService service = CreateService(dbContext);

        IReadOnlyCollection<WorkspaceDto> result = await service.GetUserWorkspacesAsync(TaskServiceTestFactory.MemberId);

        result.Should().ContainSingle(workspace => workspace.Id == otherWorkspace.Id);
        result.Should().NotContain(workspace => workspace.Id == ownedWorkspace.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowForbiddenAccessException_WhenCurrentUserIsNotOwner()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        TaskServiceTestFactory.AddWorkspaceMember(dbContext, workspace.Id, TaskServiceTestFactory.MemberId, WorkspaceRole.Member);
        await dbContext.SaveChangesAsync();
        WorkspaceService service = CreateService(dbContext);

        Func<Task> act = () => service.UpdateAsync(
            workspace.Id,
            new UpdateWorkspaceRequest("Updated", null),
            TaskServiceTestFactory.MemberId);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrowBusinessRuleException_WhenAddingOwnerRole()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        WorkspaceService service = CreateService(dbContext);

        Func<Task> act = () => service.AddMemberAsync(
            workspace.Id,
            new AddWorkspaceMemberRequest(TaskServiceTestFactory.MemberId, WorkspaceRole.Owner),
            TaskServiceTestFactory.OwnerId);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveWorkspace_WhenCurrentUserIsOwner()
    {
        await using var dbContext = TaskServiceTestFactory.CreateDbContext();
        var workspace = TaskServiceTestFactory.AddWorkspace(dbContext);
        WorkspaceService service = CreateService(dbContext);

        await service.DeleteAsync(workspace.Id, TaskServiceTestFactory.OwnerId);

        dbContext.Workspaces.Should().NotContain(entity => entity.Id == workspace.Id);
    }

    private static WorkspaceService CreateService(Flowist.TaskService.Data.TaskServiceDbContext dbContext)
    {
        return new WorkspaceService(
            dbContext,
            TaskServiceTestFactory.CreatePublishEndpoint(),
            TaskServiceTestFactory.CreateLogger<WorkspaceService>());
    }
}