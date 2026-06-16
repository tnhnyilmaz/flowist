using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record UpdateWorkspaceMemberRoleRequest(
    WorkspaceRole Role);