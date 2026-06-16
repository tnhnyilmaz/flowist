using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record WorkspaceMemberDto(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role,
    DateTimeOffset JoinedAt);