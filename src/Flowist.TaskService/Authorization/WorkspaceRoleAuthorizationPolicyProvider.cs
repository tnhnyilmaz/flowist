using Flowist.Shared.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Flowist.TaskService.Authorization;

public sealed class WorkspaceRoleAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public WorkspaceRoleAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {

    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequireWorkspaceRoleAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }


        string rolesPart = policyName[RequireWorkspaceRoleAttribute.PolicyPrefix.Length..];

        WorkspaceRole[] roles = rolesPart
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(role => Enum.Parse<WorkspaceRole>(role, ignoreCase: true))
            .ToArray();

        AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new WorkspaceRoleRequirement(roles))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}