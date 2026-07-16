using System.Net;
using System.Net.Http.Json;

using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.IntegrationTests.TestSupport;

using FluentAssertions;

using TaskStatus = Flowist.Shared.Enums.TaskStatus;

namespace Flowist.TaskService.IntegrationTests.Workflows;

public sealed class TaskWorkflowIntegrationTests : IClassFixture<TaskServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _outsiderId = Guid.NewGuid();

    public TaskWorkflowIntegrationTests(TaskServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WorkspaceCrud_ShouldCreateListUpdateAndDeleteWorkspace()
    {
        UseUser(_ownerId);

        WorkspaceDto workspace = await CreateWorkspaceAsync("Workspace CRUD");

        IReadOnlyCollection<WorkspaceDto>? workspaces = await _client.GetFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>("/api/workspaces");
        workspaces.Should().Contain(item => item.Id == workspace.Id);

        UpdateWorkspaceRequest updateRequest = new("Workspace CRUD Updated", "updated description");
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        WorkspaceDto updatedWorkspace = (await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>())!;
        updatedWorkspace.Name.Should().Be(updateRequest.Name);
        updatedWorkspace.Description.Should().Be(updateRequest.Description);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/workspaces/{workspace.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ProjectAndTaskCrud_ShouldCreateUpdateListAndDeleteTask()
    {
        UseUser(_ownerId);

        WorkspaceDto workspace = await CreateWorkspaceAsync("Project Task CRUD");
        ProjectDto project = await CreateProjectAsync(workspace.Id, "Integration Project");

        CreateTaskRequest createTaskRequest = new(
            "Integration Task",
            "task description",
            TaskPriority.High,
            null,
            DateTimeOffset.UtcNow.AddDays(1));

        HttpResponseMessage taskResponse = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTaskRequest);
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        TaskItemDto task = (await taskResponse.Content.ReadFromJsonAsync<TaskItemDto>())!;
        task.Title.Should().Be(createTaskRequest.Title);
        task.Status.Should().Be(TaskStatus.Todo);

        UpdateTaskStatusRequest statusRequest = new(TaskStatus.Done);
        HttpResponseMessage statusResponse = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", statusRequest);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        TaskItemDto updatedTask = (await statusResponse.Content.ReadFromJsonAsync<TaskItemDto>())!;
        updatedTask.Status.Should().Be(TaskStatus.Done);

        PagedResult<TaskItemDto>? tasks = await _client.GetFromJsonAsync<PagedResult<TaskItemDto>>($"/api/projects/{project.Id}/tasks");
        tasks.Should().NotBeNull();
        tasks!.Items.Should().Contain(item => item.Id == task.Id);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WorkspaceAuthorization_ShouldAllowMemberReadAndRejectOwnerOnlyMutation()
    {
        UseUser(_ownerId);

        WorkspaceDto workspace = await CreateWorkspaceAsync("Authorization Workspace");

        AddWorkspaceMemberRequest addMemberRequest = new(_memberId, WorkspaceRole.Member);
        HttpResponseMessage addMemberResponse = await _client.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/members", addMemberRequest);
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        UseUser(_memberId);
        HttpResponseMessage memberReadResponse = await _client.GetAsync($"/api/workspaces/{workspace.Id}");
        memberReadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage memberUpdateResponse = await _client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequest("Member Update Attempt", null));
        memberUpdateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        UseUser(_outsiderId);
        HttpResponseMessage outsiderReadResponse = await _client.GetAsync($"/api/workspaces/{workspace.Id}");
        outsiderReadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<WorkspaceDto> CreateWorkspaceAsync(string name)
    {
        CreateWorkspaceRequest request = new($"{name} {Guid.NewGuid():N}", "integration workspace");

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<WorkspaceDto>())!;
    }

    private async Task<ProjectDto> CreateProjectAsync(Guid workspaceId, string name)
    {
        CreateProjectRequest request = new($"{name} {Guid.NewGuid():N}", "integration project");

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/workspace/{workspaceId}/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
    }

    private void UseUser(Guid userId)
    {
        _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeaderName);
        _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString());
    }
}