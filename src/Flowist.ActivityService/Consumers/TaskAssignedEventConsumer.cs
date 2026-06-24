using System.Text.Json;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;
using Flowist.ActivityService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.ActivityService.Consumers;

public sealed class TaskAssignedEventConsumer : IConsumer<TaskAssignedEvent>
{
    private readonly ActivityDbContext _dbContext;
    private readonly ILogger<TaskAssignedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public TaskAssignedEventConsumer(
        ActivityDbContext dbContext,
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

            ActivityLog activityLog = new()
            {
                Id = Guid.NewGuid(),
                WorkspaceId = message.WorkspaceId,
                UserId = message.AssignedBy,
                ActionType = ActivityType.TaskAssigned,
                EntityType = "Task",
                EntityId = message.TaskId,
                Description = $"Task was assigned to user {message.AssignedTo}.",
                Metadata = JsonSerializer.Serialize(new
                {
                    message.TaskId,
                    message.AssignedTo,
                    message.AssignedBy,
                    message.WorkspaceId,
                    message.AssignedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ActivityLogs.Add(activityLog);

            _processedEventService.MarkAsProcessed(message.EventId, nameof(TaskAssignedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Created activity log {ActivityLogId} for task assigned event {EventId}.",
                activityLog.Id,
                message.EventId);
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