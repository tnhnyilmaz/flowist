using Flowist.Shared.Enums;

namespace Flowist.TaskService.Entities;

public sealed class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Flowist.Shared.Enums.TaskStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;

    public static implicit operator TaskItem(Project v)
    {
        throw new NotImplementedException();
    }
}