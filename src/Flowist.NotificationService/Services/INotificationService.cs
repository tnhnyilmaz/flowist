using Flowist.NotificationService.DTOs;
using Flowist.Shared.DTOs;

namespace Flowist.NotificationService.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}