using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate);