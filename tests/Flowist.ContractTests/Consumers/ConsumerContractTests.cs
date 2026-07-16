using Flowist.Shared.Events;

using FluentAssertions;

using MassTransit;

using ActivityMemberAddedEventConsumer = Flowist.ActivityService.Consumers.MemberAddedEventConsumer;
using ActivityProjectCreatedEventConsumer = Flowist.ActivityService.Consumers.ProjectCreatedEventConsumer;
using ActivityTaskAssignedEventConsumer = Flowist.ActivityService.Consumers.TaskAssignedEventConsumer;
using ActivityTaskCreatedEventConsumer = Flowist.ActivityService.Consumers.TaskCreatedEventConsumer;
using ActivityUserRegisteredEventConsumer = Flowist.ActivityService.Consumers.UserRegisteredEventConsumer;
using NotificationMemberAddedEventConsumer = Flowist.NotificationService.Consumers.MemberAddedEventConsumer;
using NotificationProjectCreatedEventConsumer = Flowist.NotificationService.Consumers.ProjectCreatedEventConsumer;
using NotificationTaskAssignedEventConsumer = Flowist.NotificationService.Consumers.TaskAssignedEventConsumer;
using NotificationTaskCreatedEventConsumer = Flowist.NotificationService.Consumers.TaskCreatedEventConsumer;

namespace Flowist.ContractTests.Consumers;

public sealed class ConsumerContractTests
{
    [Theory]
    [MemberData(nameof(NotificationConsumerContracts))]
    public void NotificationConsumers_ShouldImplementExpectedEventContract(Type consumerType, Type eventType)
    {
        consumerType.Should().BeAssignableTo(typeof(IConsumer<>).MakeGenericType(eventType));
    }

    [Theory]
    [MemberData(nameof(ActivityConsumerContracts))]
    public void ActivityConsumers_ShouldImplementExpectedEventContract(Type consumerType, Type eventType)
    {
        consumerType.Should().BeAssignableTo(typeof(IConsumer<>).MakeGenericType(eventType));
    }

    public static TheoryData<Type, Type> NotificationConsumerContracts()
    {
        return new TheoryData<Type, Type>
        {
            { typeof(NotificationTaskCreatedEventConsumer), typeof(TaskCreatedEvent) },
            { typeof(NotificationTaskAssignedEventConsumer), typeof(TaskAssignedEvent) },
            { typeof(NotificationProjectCreatedEventConsumer), typeof(ProjectCreatedEvent) },
            { typeof(NotificationMemberAddedEventConsumer), typeof(MemberAddedEvent) }
        };
    }

    public static TheoryData<Type, Type> ActivityConsumerContracts()
    {
        return new TheoryData<Type, Type>
        {
            { typeof(ActivityTaskCreatedEventConsumer), typeof(TaskCreatedEvent) },
            { typeof(ActivityTaskAssignedEventConsumer), typeof(TaskAssignedEvent) },
            { typeof(ActivityProjectCreatedEventConsumer), typeof(ProjectCreatedEvent) },
            { typeof(ActivityMemberAddedEventConsumer), typeof(MemberAddedEvent) },
            { typeof(ActivityUserRegisteredEventConsumer), typeof(UserRegisteredEvent) }
        };
    }
}