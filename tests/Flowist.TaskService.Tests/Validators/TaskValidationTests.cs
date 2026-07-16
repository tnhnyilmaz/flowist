using Flowist.Shared.Constants;
using Flowist.Shared.Enums;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Validators;

using FluentAssertions;

using FluentValidation.Results;

namespace Flowist.TaskService.Tests.Validators;

public sealed class TaskValidationTests
{
    [Fact]
    public void CreateWorkspaceValidator_ShouldPass_WhenRequestIsValid()
    {
        CreateWorkspaceValidator validator = new();

        ValidationResult result = validator.Validate(new CreateWorkspaceRequest("Workspace", "Description"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWorkspaceValidator_ShouldFail_WhenNameIsEmptyOrTooLong()
    {
        CreateWorkspaceValidator validator = new();

        validator.Validate(new CreateWorkspaceRequest("", null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateWorkspaceRequest(new string('a', ValidationConstants.WorkspaceNameMaxLength + 1), null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWorkspaceValidator_ShouldFail_WhenDescriptionIsTooLong()
    {
        UpdateWorkspaceValidator validator = new();

        ValidationResult result = validator.Validate(new UpdateWorkspaceRequest(
            "Workspace",
            new string('a', ValidationConstants.WorkspaceDescriptionMaxLength + 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddMemberValidator_ShouldFail_WhenUserIdIsEmptyOrRoleIsInvalid()
    {
        AddMemberValidator validator = new();

        validator.Validate(new AddWorkspaceMemberRequest(Guid.Empty, WorkspaceRole.Member)).IsValid.Should().BeFalse();
        validator.Validate(new AddWorkspaceMemberRequest(Guid.NewGuid(), (WorkspaceRole)999)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWorkspaceMemberRoleValidator_ShouldFail_WhenRoleIsInvalid()
    {
        UpdateWorkspaceMemberRoleValidator validator = new();

        ValidationResult result = validator.Validate(new UpdateWorkspaceMemberRoleRequest((WorkspaceRole)999));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProjectValidator_ShouldPass_WhenRequestIsValid()
    {
        CreateProjectValidator validator = new();

        ValidationResult result = validator.Validate(new CreateProjectRequest("Project", "Description"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProjectValidator_ShouldFail_WhenNameIsEmptyOrTooLong()
    {
        CreateProjectValidator validator = new();

        validator.Validate(new CreateProjectRequest("", null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateProjectRequest(new string('a', ValidationConstants.ProjectNameMaxLength + 1), null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProjectValidator_ShouldFail_WhenDescriptionIsTooLong()
    {
        UpdateProjectValidator validator = new();

        ValidationResult result = validator.Validate(new UpdateProjectRequest(
            "Project",
            new string('a', ValidationConstants.ProjectDescriptionMaxLength + 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTaskValidator_ShouldPass_WhenRequestIsValid()
    {
        CreateTaskValidator validator = new();

        ValidationResult result = validator.Validate(new CreateTaskRequest(
            "Task",
            "Description",
            TaskPriority.Medium,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTaskValidator_ShouldFail_WhenTitleIsEmptyOrTooLong()
    {
        CreateTaskValidator validator = new();

        validator.Validate(new CreateTaskRequest("", null, TaskPriority.Medium, null, null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateTaskRequest(new string('a', ValidationConstants.TaskTitleMaxLength + 1), null, TaskPriority.Medium, null, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTaskValidator_ShouldFail_WhenPriorityOrAssigneeIsInvalid()
    {
        CreateTaskValidator validator = new();

        validator.Validate(new CreateTaskRequest("Task", null, (TaskPriority)999, null, null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateTaskRequest("Task", null, TaskPriority.Medium, Guid.Empty, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTaskValidator_ShouldFail_WhenStatusPriorityOrAssigneeIsInvalid()
    {
        UpdateTaskValidator validator = new();

        validator.Validate(new UpdateTaskRequest("Task", null, (Flowist.Shared.Enums.TaskStatus)999, TaskPriority.Medium, null, null)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateTaskRequest("Task", null, Flowist.Shared.Enums.TaskStatus.Todo, (TaskPriority)999, null, null)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateTaskRequest("Task", null, Flowist.Shared.Enums.TaskStatus.Todo, TaskPriority.Medium, Guid.Empty, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignTaskValidator_ShouldFail_WhenAssigneeIdIsEmpty()
    {
        AssignTaskValidator validator = new();

        ValidationResult result = validator.Validate(new AssignTaskRequest(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTaskStatusValidator_ShouldFail_WhenStatusIsInvalid()
    {
        UpdateTaskStatusValidator validator = new();

        ValidationResult result = validator.Validate(new UpdateTaskStatusRequest((Flowist.Shared.Enums.TaskStatus)999));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TaskFilterValidator_ShouldFail_WhenPagingOrSortIsInvalid()
    {
        TaskFilterValidator validator = new();

        validator.Validate(new TaskFilterRequest(null, null, null, null, null, null, false, 0, 20)).IsValid.Should().BeFalse();
        validator.Validate(new TaskFilterRequest(null, null, null, null, null, null, false, 1, 101)).IsValid.Should().BeFalse();
        validator.Validate(new TaskFilterRequest(null, null, null, null, null, "unknown", false, 1, 20)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TaskFilterValidator_ShouldPass_WhenSortByIsAllowed()
    {
        TaskFilterValidator validator = new();

        validator.Validate(new TaskFilterRequest(null, null, null, null, null, "createdAt", false, 1, 20)).IsValid.Should().BeTrue();
        validator.Validate(new TaskFilterRequest(null, null, null, null, null, "dueDate", false, 1, 20)).IsValid.Should().BeTrue();
        validator.Validate(new TaskFilterRequest(null, null, null, null, null, "priority", false, 1, 20)).IsValid.Should().BeTrue();
    }
}