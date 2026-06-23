using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Consumers;

public sealed class TaskAssignedEventConsumer : IConsumer<TaskAssignedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<TaskAssignedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;
    private readonly INotificationRealtimeService _realtimeService;

    public TaskAssignedEventConsumer(
      NotificationDbContext dbContext,
      ILogger<TaskAssignedEventConsumer> logger,
      IProcessedEventService processedEventService,
      INotificationRealtimeService realtimeService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
        _realtimeService = realtimeService;
    }
    public async Task Consume(ConsumeContext<TaskAssignedEvent> context)
    {
        try
        {

            TaskAssignedEvent message = context.Message;

            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(TaskAssignedEvent),
                    message.EventId);

                return;
            }
            Notification notification = new()
            {
                Id = Guid.NewGuid(),
                UserId = message.AssignedTo,
                Type = NotificationType.TaskAssigned,
                Title = "Task assigned",
                Message = "A task was assigned to you.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(TaskAssignedEvent));
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            NotificationDto notificationDto = new(
                notification.Id,
                notification.UserId,
                notification.Type,
                notification.Message,
                notification.IsRead,
                notification.CreatedAt);

            await _realtimeService.SendNotificationAsync(notificationDto, context.CancellationToken);

            int unreadCount = await _dbContext.Notifications
                .CountAsync(existingNotifications =>
                existingNotifications.UserId == notification.UserId &&
                !existingNotifications.IsRead,
                context.CancellationToken);

            await _realtimeService.SendUnreadCountAsync(notification.UserId, unreadCount, context.CancellationToken);

            _logger.LogInformation(
                "Created notification {NotificationId} for assigned task {TaskId}.",
                notification.Id,
                message.TaskId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(TaskAssignedEvent),
                context.MessageId);

            throw;
        }
    }
}