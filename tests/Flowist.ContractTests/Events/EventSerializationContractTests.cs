using System.Text.Json;

using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using FluentAssertions;

namespace Flowist.ContractTests.Events;

public sealed class EventSerializationContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [MemberData(nameof(EventContracts))]
    public void IntegrationEventContracts_ShouldRoundTripThroughJson<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        string json = JsonSerializer.Serialize(integrationEvent, JsonOptions);

        TEvent? deserialized = JsonSerializer.Deserialize<TEvent>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(integrationEvent.EventId);
        deserialized.OccurredOn.Should().Be(integrationEvent.OccurredOn);
        deserialized.CorrelationId.Should().Be(integrationEvent.CorrelationId);
        deserialized.Should().BeEquivalentTo(integrationEvent);
    }

    [Fact]
    public void EventContracts_ShouldExposeEnvelopeFieldsRequiredByConsumers()
    {
        TaskCreatedEvent integrationEvent = new(
            Guid.NewGuid(),
            "Task title",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        string json = JsonSerializer.Serialize(integrationEvent, JsonOptions);
        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;
        root.TryGetProperty("eventId", out JsonElement eventId).Should().BeTrue();
        root.TryGetProperty("occurredOn", out JsonElement occurredOn).Should().BeTrue();
        root.TryGetProperty("correlationId", out JsonElement correlationId).Should().BeTrue();

        eventId.GetGuid().Should().Be(integrationEvent.EventId);
        occurredOn.GetDateTimeOffset().Should().Be(integrationEvent.OccurredOn);
        correlationId.GetGuid().Should().Be(integrationEvent.CorrelationId);
    }

    public static TheoryData<IntegrationEvent> EventContracts()
    {
        Guid correlationId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new TheoryData<IntegrationEvent>
        {
            new TaskCreatedEvent(Guid.NewGuid(), "Task created", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now, correlationId),
            new TaskAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now, correlationId),
            new ProjectCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Project created", Guid.NewGuid(), now, correlationId),
            new MemberAddedEvent(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), now, correlationId),
            new UserRegisteredEvent(Guid.NewGuid(), "user@flowist.local", "Flowist User", now, correlationId),
            new NotificationCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "TaskAssigned", "Notification message", now, correlationId)
        };
    }
}