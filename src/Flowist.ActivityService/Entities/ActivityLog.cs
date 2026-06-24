using Flowist.Shared.Enums;

namespace Flowist.ActivityService.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public ActivityType ActionType { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}