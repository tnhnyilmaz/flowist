using Flowist.ActivityService.DTOs;
using Flowist.Shared.DTOs;

namespace Flowist.ActivityService.Services;

public interface IActivityService
{
    Task<PagedResult<ActivityLogDto>> GetWorkspaceActivitiesAsync(Guid workspaceId, ActivityQueryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ActivityLogDto>> GetWorkspaceAuditActivitiesAsync(Guid workspaceId, ActivityQueryRequest request, CancellationToken cancellationToken = default);
}