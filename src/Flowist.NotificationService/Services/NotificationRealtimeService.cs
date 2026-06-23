using Flowist.NotificationService.Hubs;
using Flowist.Shared.DTOs;

using Microsoft.AspNetCore.SignalR;

namespace Flowist.NotificationService.Services;

public sealed class NotificationRealtimeService : INotificationRealtimeService
{

    private readonly IHubContext<NotificationHub> _hubContext;
    public NotificationRealtimeService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        await _hubContext
            .Clients
            .Group(NotificationHub.GetUserGroupName(notification.UserId))
            .SendAsync("NotificationCreated", notification, cancellationToken);
    }

    public async Task SendUnreadCountAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default)
    {
        await _hubContext
            .Clients
            .Group(NotificationHub.GetUserGroupName(userId))
            .SendAsync("UnreadCountUpdated", unreadCount, cancellationToken);
    }
}