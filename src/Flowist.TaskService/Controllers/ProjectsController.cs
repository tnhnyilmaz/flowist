using System.Security.Claims;

using Flowist.Shared.DTOs;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowist.TaskService.Controllers;

[ApiController]
[Authorize]
public sealed class ProjectsController : Controller
{

    private readonly IProjectService _projectService;
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }



    [HttpPost("api/workspace/{workspaceId:guid}/projects")]
    public async Task<ActionResult<ProjectDto>> Create(
        Guid workspaceId,
        CreateProjectRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        ProjectDto project = await _projectService.CreateAsync(
            workspaceId,
            request,
            currentUserId,
            cancellationToken
        );

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }


    [HttpGet("api/workspaces/{workspaceId:guid}/projects")]
    public async Task<ActionResult<IReadOnlyCollection<ProjectDto>>> GetByWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        IReadOnlyCollection<ProjectDto> projects = await _projectService.GetWorkspaceProjectsAsync(
            workspaceId,
            currentUserId,
            cancellationToken
        );

        return Ok(projects);

    }



    [HttpGet("api/projects/{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {

        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        ProjectDto project = await _projectService.GetByIdAsync(
            id,
            currentUserId,
            cancellationToken
        );
        return Ok(project);
    }

    [HttpPut("api/projects/{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        ProjectDto project = await _projectService.UpdateAsync(
            id,
            request,
            currentUserId,
            cancellationToken
        );
        return Ok(project);
    }


    [HttpDelete("api/projects/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _projectService.DeleteAsync(
            id,
            currentUserId,
            cancellationToken
        );

        return NoContent();

    }


    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? usedIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        return Guid.TryParse(usedIdClaim, out userId);
    }








}