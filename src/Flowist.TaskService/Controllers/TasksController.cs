using System.Security.Claims;

using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.TaskService.Authorization;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowist.TaskService.Controllers;

[ApiController]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskItemService;

    public TasksController(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    /// <summary>
    /// Creates a task in a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="request">The task creation request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created task.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpPost("api/projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskItemDto>> Create(
    Guid projectId,
    CreateTaskRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        TaskItemDto task = await _taskItemService.CreateAsync(
            projectId,
            request,
            currentUserId,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }



    /// <summary>
    /// Gets paged tasks in a project with optional filtering and sorting.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="filter">The task filtering, sorting and pagination options.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The paged project tasks.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpGet("api/projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<PagedResult<TaskItemDto>>> GetByProject(
         Guid projectId,
         [FromQuery] TaskFilterRequest filter,
         CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        PagedResult<TaskItemDto> tasks = await _taskItemService.GetProjectTasksAsync(
            projectId,
            filter,
            currentUserId,
            cancellationToken);

        return Ok(tasks);
    }



    /// <summary>
    /// Gets a task by id if the authenticated user is a member of its workspace.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The requested task.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpGet("api/tasks/{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        TaskItemDto task = await _taskItemService.GetByIdAsync(
            id,
            currentUserId,
            cancellationToken);

        return Ok(task);
    }


    /// <summary>
    /// Updates a task.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="request">The task update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated task.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpPut("api/tasks/{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Update(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        TaskItemDto task = await _taskItemService.UpdateAsync(
            id,
            request,
            currentUserId,
            cancellationToken);

        return Ok(task);
    }


    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the task is deleted.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpDelete("api/tasks/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        await _taskItemService.DeleteAsync(
            id,
            currentUserId,
            cancellationToken);

        return NoContent();
    }



    /// <summary>
    /// Assigns a task to a workspace member. Requires workspace owner or admin role.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="request">The task assignment request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The assigned task.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin)]
    [HttpPut("api/tasks/{id:guid}/assign")]
    public async Task<ActionResult<TaskItemDto>> Assign(
        Guid id,
        AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        TaskItemDto task = await _taskItemService.AssignAsync(
            id,
            request,
            currentUserId,
            cancellationToken);

        return Ok(task);
    }


    /// <summary>
    /// Updates a task status.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="request">The task status update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated task.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpPut("api/tasks/{id:guid}/status")]
    public async Task<ActionResult<TaskItemDto>> UpdateStatus(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        TaskItemDto task = await _taskItemService.UpdateStatusAsync(
            id,
            request,
            currentUserId,
            cancellationToken);

        return Ok(task);
    }
    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? userIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        return Guid.TryParse(userIdClaim, out userId);
    }
}