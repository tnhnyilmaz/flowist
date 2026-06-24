using System.Security.Principal;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.DTOs;
using Flowist.ActivityService.Entities;
using Flowist.Shared.DTOs;

using Microsoft.EntityFrameworkCore;

namespace Flowist.ActivityService.Services;


public sealed class ActivityService : IActivityService
{
    private readonly ActivityDbContext _dbContext;

    public ActivityService(ActivityDbContext activityDbContext)
    {
        _dbContext = activityDbContext;
    }

    public async Task<PagedResult<ActivityLogDto>> GetWorkspaceActivitiesAsync(Guid workspaceId, ActivityQueryRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<ActivityLog> query = _dbContext.ActivityLogs
            .AsNoTracking()
            .Where(activityLog => activityLog.WorkspaceId == workspaceId);

        query = ApplyFiltering(query, request);

        return await ToPagedResultAsync(query, request, cancellationToken);
    }

    public async Task<PagedResult<ActivityLogDto>> GetWorkspaceAuditActivitiesAsync(Guid workspaceId, ActivityQueryRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<ActivityLog> query = _dbContext.ActivityLogs
            .AsNoTracking()
            .Where(activityLog => activityLog.WorkspaceId == workspaceId);

        query = ApplyFiltering(query, request);

        return await ToPagedResultAsync(query, request, cancellationToken);
    }



    private async Task<PagedResult<ActivityLogDto>> ToPagedResultAsync(IQueryable<ActivityLog> query, ActivityQueryRequest request, CancellationToken cancellationToken)
    {
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        query = query.OrderByDescending(activityLog => activityLog.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);

        List<ActivityLog> activityLogs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityLogDto>(
            activityLogs.Select(ToActivityLogDto).ToArray(),
            totalCount,
            page,
            pageSize);
    }



    private static IQueryable<ActivityLog> ApplyFiltering(IQueryable<ActivityLog> query, ActivityQueryRequest request)
    {
        if (request.ActionType.HasValue)
        {
            query = query.Where(activityLog => activityLog.ActionType == request.ActionType.Value);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(activityLog => activityLog.UserId == request.UserId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(activityLog => activityLog.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(activityLog => activityLog.CreatedAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            string entityType = request.EntityType.Trim();

            query = query.Where(activityLog => activityLog.EntityType == entityType);
        }

        return query;
    }
    private static ActivityLogDto ToActivityLogDto(ActivityLog activityLog)
    {
        return new ActivityLogDto(
            activityLog.Id,
            activityLog.WorkspaceId ?? Guid.Empty,
            activityLog.UserId,
            activityLog.ActionType,
            activityLog.Description,
            activityLog.CreatedAt);
    }
}