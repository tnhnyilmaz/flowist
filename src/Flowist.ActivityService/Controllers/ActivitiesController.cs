


using Flowist.ActivityService.DTOs;
using Flowist.ActivityService.Services;
using Flowist.Shared.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowist.ActivityService.Controllers;

[ApiController]
[Authorize]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivitiesController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>
    /// Gets paged workspace activity feed entries.
    /// </summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="request">The activity filtering and pagination request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The paged workspace activity feed.</returns>

    [HttpGet("api/workspaces/{workspaceId:guid}/activities")]
    public async Task<ActionResult<PagedResult<ActivityLogDto>>> GetWorkspaceActivities(
    Guid workspaceId,
    [FromQuery] ActivityQueryRequest request,
    CancellationToken cancellationToken)
    {
        PagedResult<ActivityLogDto> activities = await _activityService.GetWorkspaceActivitiesAsync(
            workspaceId,
            request,
            cancellationToken
        );

        return Ok(activities);
    }



    /// <summary>
    /// Gets paged workspace audit trail entries.
    /// </summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="request">The activity filtering and pagination request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The paged workspace audit trail.</returns>
    [HttpGet("api/workspaces/{workspaceId:guid}/activities/audit")]
    public async Task<ActionResult<PagedResult<ActivityLogDto>>> GetWorkspaceAuditActivities(
        Guid workspaceId,
        [FromQuery] ActivityQueryRequest request,
        CancellationToken cancellationToken)
    {
        PagedResult<ActivityLogDto> activities = await _activityService.GetWorkspaceAuditActivitiesAsync(
            workspaceId,
            request,
            cancellationToken);

        return Ok(activities);
    }

}