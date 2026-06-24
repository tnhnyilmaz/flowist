using System.Text.Json;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;
using Flowist.ActivityService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.ActivityService.Consumers;

public sealed class ProjectCreatedEventConsumer : IConsumer<ProjectCreatedEvent>
{
    private readonly ActivityDbContext _dbContext;
    private readonly ILogger<ProjectCreatedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public ProjectCreatedEventConsumer(
        ActivityDbContext dbContext,
        ILogger<ProjectCreatedEventConsumer> logger,
        IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _processedEventService = processedEventService;
        _logger = logger;
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

            ActivityLog activityLog = new()
            {
                Id = Guid.NewGuid(),
                WorkspaceId = message.WorkspaceId,
                UserId = message.CreatedBy,
                ActionType = ActivityType.ProjectCreated,
                EntityType = "Project",
                EntityId = message.ProjectId,
                Description = $"Project '{message.Name}' was created.",
                Metadata = JsonSerializer.Serialize(new
                {
                    message.ProjectId,
                    message.WorkspaceId,
                    message.Name,
                    message.CreatedBy,
                    message.CreatedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ActivityLogs.Add(activityLog);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(ProjectCreatedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation(
                "Created activity log {ActivityLogId} for project created event {EventId}.",
                activityLog.Id,
                message.EventId);
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