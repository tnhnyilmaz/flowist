using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.NotificationService.Consumers;

public sealed class TaskAssignedEventConsumer : IConsumer<TaskAssignedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<TaskAssignedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public TaskAssignedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<TaskAssignedEventConsumer> logger,
        IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
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