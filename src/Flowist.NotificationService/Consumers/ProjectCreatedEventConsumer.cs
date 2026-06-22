using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.NotificationService.Consumers;

public sealed class ProjectCreatedEventConsumer : IConsumer<ProjectCreatedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<ProjectCreatedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public ProjectCreatedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<ProjectCreatedEventConsumer> logger,
        IProcessedEventService processedEventService)

    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
    }

    public async Task Consume(ConsumeContext<ProjectCreatedEvent> context)
    {
        try
        {
            ProjectCreatedEvent message = context.Message;


            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(ProjectCreatedEvent),
                    message.EventId);

                return;
            }

            Notification notification = new()
            {
                Id = Guid.NewGuid(),
                UserId = message.CreatedBy,
                Type = NotificationType.ProjectCreated,
                Title = "Project created",
                Message = $"Project '{message.Name}' was created.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notifications.Add(notification);

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(ProjectCreatedEvent));

            _logger.LogInformation(
                "Created notification {NotificationId} for created project {ProjectId}.",
                notification.Id,
                message.ProjectId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(ProjectCreatedEvent),
                context.MessageId);

            throw;
        }
    }
}