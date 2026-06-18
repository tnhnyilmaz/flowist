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
public sealed class ProjectsController : Controller
{

    private readonly IProjectService _projectService;
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Creates a project in a workspace. Requires workspace owner or admin role.
    /// </summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="request">The project creation request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created project.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin)]
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



    /// <summary>
    /// Gets all projects in a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The workspace projects.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
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



    /// <summary>
    /// Gets a project by id if the authenticated user is a member of its workspace.
    /// </summary>
    /// <param name="id">The project id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The requested project.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
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


    /// <summary>
    /// Updates a project. Requires workspace owner or admin role.
    /// </summary>
    /// <param name="id">The project id.</param>
    /// <param name="request">The project update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated project.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin)]
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



    /// <summary>
    /// Deletes a project. Requires workspace owner or admin role.
    /// </summary>
    /// <param name="id">The project id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the project is deleted.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin)]
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