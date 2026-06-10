using Flowist.Shared.Enums;

namespace Flowist.Shared.DTOs;

public sealed record TaskItemDto(
    Guid Id,
    string Title,
    string? Description,
    Flowist.Shared.Enums.TaskStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    Guid ProjectId,
    DateTimeOffset? DueDate);