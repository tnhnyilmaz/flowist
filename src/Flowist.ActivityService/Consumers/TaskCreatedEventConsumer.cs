using System.Text.Json;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;
using Flowist.ActivityService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.ActivityService.Consumers;

public sealed class TaskCreatedEventConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly ActivityDbContext _dbContext;
    private readonly ILogger<TaskCreatedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;


    public TaskCreatedEventConsumer(
        ActivityDbContext dbContext,
        ILogger<TaskCreatedEventConsumer> logger, IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
    }

    public async Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        try
        {
            TaskCreatedEvent message = context.Message;

            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(TaskCreatedEvent),
                    message.EventId);

                return;
            }

            ActivityLog activityLog = new()
            {
                Id = Guid.NewGuid(),
                WorkspaceId = message.WorkspaceId,
                UserId = message.CreatedBy,
                ActionType = ActivityType.TaskCreated,
                EntityType = "Task",
                EntityId = message.TaskId,
                Description = $"Task '{message.Title}' was created.",
                Metadata = JsonSerializer.Serialize(new
                {
                    message.TaskId,
                    message.Title,
                    message.ProjectId,
                    message.WorkspaceId,
                    message.CreatedBy,
                    message.CreatedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ActivityLogs.Add(activityLog);

            _processedEventService.MarkAsProcessed(message.EventId, nameof(TaskCreatedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Created activity log {ActivityLogId} for task created event {EventId}.",
                activityLog.Id,
                message.EventId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(TaskCreatedEvent),
                context.MessageId);

            throw;
        }
    }
}