
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
[Route("api/[controller]")]
public sealed class WorkspaceController : Controller
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspaceController(IWorkspaceService workspaceService)
    {

        _workspaceService = workspaceService;
    }


    /// <summary>
    /// Creates a new workspace and assigns the authenticated user as owner.
    /// </summary>
    /// <param name="request">The workspace creation request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created workspace.</returns>
    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create(
    CreateWorkspaceRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId))
        {
            return Unauthorized();
        }

        WorkspaceDto workspace = await _workspaceService.CreateAsync(
            request,
            currentUserId,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, workspace);
    }


    /// <summary>
    /// Gets all workspaces where the authenticated user is a member.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authenticated user's workspaces.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        IReadOnlyCollection<WorkspaceDto> workspaces = await _workspaceService.GetUserWorkspacesAsync(currentUserId, cancellationToken);

        return Ok(workspaces);
    }


    /// <summary>
    /// Gets a workspace by id if the authenticated user is a member.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The requested workspace.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        WorkspaceDto workspace = await _workspaceService.GetByIdAsync(
            id,
            currentUserId,
            cancellationToken
        );
        return Ok(workspace);
    }


    /// <summary>
    /// Updates a workspace. Requires workspace owner role.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="request">The workspace update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated workspace.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Update(
            Guid id,
            UpdateWorkspaceRequest request,
            CancellationToken cancellationToken
        )
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        WorkspaceDto workspace = await _workspaceService.UpdateAsync(
            id,
            request,
            currentUserId,
            cancellationToken
        );

        return Ok(workspace);
    }

    /// <summary>
    /// Deletes a workspace. Requires workspace owner role.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the workspace is deleted.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _workspaceService.DeleteAsync(
            id,
            currentUserId,
            cancellationToken
        );

        return NoContent();

    }



    /// <summary>
    /// Adds a member to a workspace. Requires workspace owner role.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="request">The member creation request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created workspace member.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<WorkspaceMemberDto>> AddMember(Guid id, AddWorkspaceMemberRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        WorkspaceMemberDto member = await _workspaceService.AddMemberAsync(
            id,
            request,
            currentUserId,
            cancellationToken
        );

        return CreatedAtAction(nameof(GetMembers), new { id }, member);

    }




    /// <summary>
    /// Removes a member from a workspace. Requires workspace owner role.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="userId">The user id to remove from the workspace.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the member is removed.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _workspaceService.RemoveMemberAsync(
            id,
            userId,
            currentUserId,
            cancellationToken
        );

        return NoContent();


    }




    /// <summary>
    /// Updates a workspace member role. Requires workspace owner role.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="userId">The member user id.</param>
    /// <param name="request">The role update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated workspace member.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<WorkspaceMemberDto>> UpdateMemberRole(Guid id, Guid userId, UpdateWorkspaceMemberRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        WorkspaceMemberDto member = await _workspaceService.UpdateMemberRoleAsync(
            id,
            userId,
            request,
            currentUserId,
            cancellationToken
        );
        return Ok(member);
    }



    /// <summary>
    /// Gets all members of a workspace.
    /// </summary>
    /// <param name="id">The workspace id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The workspace members.</returns>
    [RequireWorkspaceRole(WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member)]
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceMemberDto>>> GetMembers(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        IReadOnlyCollection<WorkspaceMemberDto> members = await _workspaceService.GetMembersAsync(
            id,
            currentUserId,
            cancellationToken
        );
        return Ok(members);
    }









    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? userIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);
        return Guid.TryParse(userIdClaim, out userId);
    }
}