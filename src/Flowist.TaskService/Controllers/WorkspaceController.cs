
using System.Security.Claims;

using Flowist.Shared.DTOs;
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



    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        IReadOnlyCollection<WorkspaceDto> workspaces = await _workspaceService.GetUserWorkspacesAsync(currentUserId, cancellationToken);

        return Ok(workspaces);
    }




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