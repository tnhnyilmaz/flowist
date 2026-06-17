using System.Security.Claims;

using Flowist.Shared.DTOs;
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