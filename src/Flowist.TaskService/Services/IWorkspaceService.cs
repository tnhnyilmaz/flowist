using Flowist.Shared.DTOs;
using Flowist.TaskService.DTOs;

namespace Flowist.TaskService.Services;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateAsync(CreateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkspaceDto>> GetUserWorkspacesAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task<WorkspaceDto> GetByIdAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<WorkspaceDto> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<WorkspaceMemberDto> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(Guid workspaceId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<WorkspaceMemberDto> UpdateMemberRoleAsync(Guid workspaceId, Guid userId, UpdateWorkspaceMemberRoleRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);
}