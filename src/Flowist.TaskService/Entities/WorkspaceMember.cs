using Flowist.Shared.Enums;

namespace Flowist.TaskService.Entities;

public sealed class WorkspaceMember
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public WorkspaceRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
}