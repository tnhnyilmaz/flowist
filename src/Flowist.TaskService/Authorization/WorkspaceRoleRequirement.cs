using Flowist.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

namespace Flowist.TaskService.Authorization;

public sealed class WorkspaceRoleRequirement : IAuthorizationRequirement
{
    public WorkspaceRoleRequirement(params WorkspaceRole[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public IReadOnlyCollection<WorkspaceRole> AllowedRoles { get; }
}