using Flowist.Shared.DTOs;
using Flowist.TaskService.DTOs;

namespace Flowist.TaskService.Services;

public interface ITaskItemService
{
    Task<TaskItemDto> CreateAsync(Guid projectId, CreateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<PagedResult<TaskItemDto>> GetProjectTasksAsync(Guid projectId, TaskFilterRequest filter, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<TaskItemDto> GetByIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<TaskItemDto> UpdateAsync(Guid taskId, UpdateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<TaskItemDto> AssignAsync(Guid taskId, AssignTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<TaskItemDto> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
}