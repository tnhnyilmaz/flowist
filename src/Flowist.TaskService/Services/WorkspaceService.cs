using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.Data;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Entities;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Flowist.TaskService.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly TaskServiceDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WorkspaceService> _logger;
    public WorkspaceService(
    TaskServiceDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<WorkspaceService> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }




    public async Task<WorkspaceMemberDto> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerAsync(workspaceId, currentUserId, cancellationToken);

        if (request.Role == WorkspaceRole.Owner)
        {
            throw new BusinessRuleException("New members cannot be added as workspace owner.");
        }

        bool workspaceExists = await _dbContext.Workspaces
            .AnyAsync(workspace => workspace.Id == workspaceId, cancellationToken);

        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), workspaceId);

        bool alreadyMember = await _dbContext.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == request.UserId, cancellationToken);

        if (alreadyMember) throw new ConflictException("Workspace member already exists");

        WorkspaceMember member = new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _dbContext.WorkspaceMembers.Add(member);

        await _dbContext.SaveChangesAsync(cancellationToken);

        MemberAddedEvent memberAddedEvent = new(
            member.WorkspaceId,
            member.UserId,
            member.Role,
            currentUserId,
            member.JoinedAt,
            Guid.NewGuid());

        await PublishEventAsync(memberAddedEvent, cancellationToken);


        return ToWorkspaceMemberDto(member);
    }

    public async Task<WorkspaceDto> CreateAsync(CreateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        string normalizedName = request.Name.Trim();

        bool workspaceNameExists = await _dbContext.Workspaces
            .AnyAsync(workspace => workspace.OwnerId == currentUserId &&
            workspace.Name == normalizedName,
            cancellationToken);

        if (workspaceNameExists) throw new ConflictException("Workspace name already axists");

        Workspace workspace = new()
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Description = request.Description,
            OwnerId = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        WorkspaceMember ownerMember = new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = currentUserId,
            Role = WorkspaceRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow
        };

        workspace.Members.Add(ownerMember);
        _dbContext.Workspaces.Add(workspace);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToWorkspaceDto(workspace);


    }

    public async Task DeleteAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerAsync(workspaceId, currentUserId, cancellationToken);

        Workspace workspace = await _dbContext.Workspaces
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), workspaceId);

        _dbContext.Workspaces.Remove(workspace);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkspaceDto> GetByIdAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Workspace workspace = await GetWorkspaceForMemberAsync(workspaceId, currentUserId, cancellationToken);

        return ToWorkspaceDto(workspace);
    }

    public async Task<IReadOnlyCollection<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(workspaceId, currentUserId, cancellationToken);

        List<WorkspaceMember> members = await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId)
            .OrderBy(member => member.JoinedAt)
            .ToListAsync(cancellationToken);

        return members.Select(ToWorkspaceMemberDto).ToArray();

    }

    public async Task<IReadOnlyCollection<WorkspaceDto>> GetUserWorkspacesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        List<Workspace> workspaces = await _dbContext.WorkspaceMembers
                 .AsNoTracking()
                 .Where(member => member.UserId == currentUserId)
                 .Select(member => member.Workspace)
                 .OrderBy(workspace => workspace.Name)
                 .ToListAsync(cancellationToken);

        return workspaces.Select(ToWorkspaceDto).ToArray();
    }

    public async Task RemoveMemberAsync(Guid workspaceId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerAsync(workspaceId, currentUserId, cancellationToken);

        Workspace workspace = await _dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId == userId)
        {
            throw new BusinessRuleException("Workspadce owner cannot be removed");
        }

        WorkspaceMember member = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkspaceMember), userId);

        _dbContext.WorkspaceMembers.Remove(member);
        await _dbContext.SaveChangesAsync(cancellationToken);

    }

    public async Task<WorkspaceDto> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerAsync(workspaceId, currentUserId, cancellationToken);

        string normalizedName = request.Name.Trim();

        bool workspaceNameExists = await _dbContext.Workspaces
            .AnyAsync(workspace =>
                workspace.OwnerId == currentUserId &&
                workspace.Id != workspaceId &&
                workspace.Name == normalizedName,
                cancellationToken);

        if (workspaceNameExists)
        {
            throw new ConflictException("Workspace name already exists.");
        }

        Workspace workspace = await _dbContext.Workspaces
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), workspaceId);

        workspace.Name = normalizedName;
        workspace.Description = request.Description;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToWorkspaceDto(workspace);
    }

    private async Task EnsureOwnerAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken)
    {
        bool isOwner = await _dbContext.WorkspaceMembers
            .AnyAsync(member =>
            member.WorkspaceId == workspaceId &&
            member.UserId == currentUserId &&
            member.Role == WorkspaceRole.Owner
            );

        if (!isOwner) throw new ForbiddenAccessException("Only workspace owners can perform this action.");

    }

    public async Task<WorkspaceMemberDto> UpdateMemberRoleAsync(Guid workspaceId, Guid userId, UpdateWorkspaceMemberRoleRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerAsync(workspaceId, currentUserId, cancellationToken);

        if (request.Role == WorkspaceRole.Owner)
        {
            throw new BusinessRuleException("Workspace owner role cannot be assigned manually.");
        }

        Workspace workspace = await _dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId == userId) throw new BusinessRuleException("Workspace owner role cannot be changed");

        WorkspaceMember member = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkspaceMember), userId);

        member.Role = request.Role;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToWorkspaceMemberDto(member);

    }

    private static WorkspaceDto ToWorkspaceDto(Workspace workspace)
    {
        return new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.OwnerId,
            workspace.CreatedAt);
    }
    private async Task<Workspace> GetWorkspaceForMemberAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken)
    {
        bool isMember = await _dbContext.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == currentUserId);

        if (!isMember) throw new ForbiddenAccessException();

        return await _dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), workspaceId);
    }
    private static WorkspaceMemberDto ToWorkspaceMemberDto(WorkspaceMember member)
    {
        return new WorkspaceMemberDto(
            member.Id,
            member.WorkspaceId,
            member.UserId,
            member.Role,
            member.JoinedAt);
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken)
    {
        bool isMember = await _dbContext.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == currentUserId, cancellationToken);

        if (!isMember) throw new ForbiddenAccessException();
    }


    private async Task PublishEventAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
    {
        try
        {
            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to publish integration event {EventType}.", typeof(TEvent).Name);
            throw;
        }
    }

}