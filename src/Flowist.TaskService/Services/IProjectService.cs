using Flowist.Shared.DTOs;
using Flowist.TaskService.DTOs;

namespace Flowist.TaskService.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(Guid workspaceId, CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProjectDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ProjectDto> GetByIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
}