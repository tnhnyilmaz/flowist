using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    Flowist.Shared.Enums.TaskStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate);