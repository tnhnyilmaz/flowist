using Flowist.Shared.DTOs;

namespace Flowist.NotificationService.Services;

public interface INotificationRealtimeService
{
    Task SendNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default);

    Task SendUnreadCountAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default);
}