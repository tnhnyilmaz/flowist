using Flowist.Shared.Enums;

namespace Flowist.NotificationService.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}