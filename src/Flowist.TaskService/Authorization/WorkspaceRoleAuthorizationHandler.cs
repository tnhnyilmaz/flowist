
using System.Security.Claims;

using Flowist.Shared.Enums;
using Flowist.TaskService.Data;
using Flowist.TaskService.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Flowist.TaskService.Authorization;

public sealed class WorkspaceRoleAuthorizationHandler : AuthorizationHandler<WorkspaceRoleRequirement>
{

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TaskServiceDbContext _dbContext;


    public WorkspaceRoleAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor, TaskServiceDbContext dbContext
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }



    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspaceRoleRequirement requirement)
    {

        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return;
        }

        string? userIdClaim = context.User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        if (!Guid.TryParse(userIdClaim, out Guid userId)) return;

        Guid? workspaceId = await ResolveWorkspaceIdAsync(httpContext, httpContext.RequestAborted);

        if (!workspaceId.HasValue) return;

        WorkspaceRole? role = await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId.Value && member.UserId == userId)
            .Select(member => (WorkspaceRole?)member.Role)
            .FirstOrDefaultAsync(httpContext.RequestAborted);

        if (role.HasValue && requirement.AllowedRoles.Contains(role.Value)) context.Succeed(requirement);

    }

    private async Task<Guid?> ResolveWorkspaceIdAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        RouteValueDictionary routeValues = httpContext.GetRouteData().Values;

        if (TryGetGuidRouteValue(routeValues, "workspaceId", out Guid workspaceId)) return workspaceId;

        if (TryGetGuidRouteValue(routeValues, "projectId", out Guid projectId))
        {
            Project? project = await _dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);

            return project?.WorkspaceId;
        }

        if (!TryGetGuidRouteValue(routeValues, "id", out Guid id)) return null;

        string path = httpContext.Request.Path.Value ?? string.Empty;

        if (path.Contains("/projects/", StringComparison.OrdinalIgnoreCase))
        {
            Project? project = await _dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
            return project?.WorkspaceId;

        }
        if (path.Contains("/tasks/", StringComparison.OrdinalIgnoreCase))
        {
            TaskItem? task = await _dbContext.TaskItems
                .AsNoTracking()
                .Include(task => task.Project)
                .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

            return task?.Project.WorkspaceId;
        }

        if (path.Contains("/workspaces/", StringComparison.OrdinalIgnoreCase))
        {
            return id;
        }

        return null;
    }

    private static bool TryGetGuidRouteValue(RouteValueDictionary routeValues, string key, out Guid value)
    {
        value = Guid.Empty;

        if (!routeValues.TryGetValue(key, out object? routeValue))
        {
            return false;
        }

        return Guid.TryParse(Convert.ToString(routeValue), out value);
    }
}