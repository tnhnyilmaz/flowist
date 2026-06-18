using Flowist.Shared.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Flowist.TaskService.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireWorkspaceRoleAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "WorkspaceRole:";

    public RequireWorkspaceRoleAttribute(params WorkspaceRole[] roles)
    {
        Roles = roles;
        Policy = BuildPolicyName(roles);
    }

    public new IReadOnlyCollection<WorkspaceRole> Roles { get; }

    public static string BuildPolicyName(IEnumerable<WorkspaceRole> roles)
    {
        return PolicyPrefix + string.Join(",", roles.Select(role => role.ToString()));
    }
}