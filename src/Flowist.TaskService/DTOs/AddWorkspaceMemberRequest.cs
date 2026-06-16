using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record AddWorkspaceMemberRequest(
    Guid UserId,
    WorkspaceRole Role);