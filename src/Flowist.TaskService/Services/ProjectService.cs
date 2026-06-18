using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.Data;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Entities;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Flowist.TaskService.Services;

public sealed class ProjectService : IProjectService
{
    private readonly TaskServiceDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProjectService> _logger;
    public ProjectService(
    TaskServiceDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }


    public async Task<ProjectDto> CreateAsync(Guid workspaceId, CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceRoleAsync(workspaceId, currentUserId, [WorkspaceRole.Owner, WorkspaceRole.Admin], cancellationToken);

        string normalizedName = request.Name.Trim();

        bool projectNameExists = await _dbContext.Projects
            .AnyAsync(project =>
                project.WorkspaceId == workspaceId &&
                project.Name == normalizedName,
                cancellationToken);

        if (projectNameExists) throw new ConflictException("Project name already exists.");

        Project project = new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = normalizedName,
            Description = request.Description?.Trim(),
            CreatedBy = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Projects.Add(project);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToProjectDto(project);

    }


    public async Task DeleteAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {


        Project project = await GetProjectForWorkspaceMemberAsync(projectId, currentUserId, cancellationToken);
        await EnsureWorkspaceRoleAsync(project.WorkspaceId, currentUserId, [WorkspaceRole.Owner, WorkspaceRole.Admin], cancellationToken);

        _dbContext.Projects.Remove(project);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<ProjectDto> GetByIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Project project = await GetProjectForWorkspaceMemberAsync(projectId, currentUserId, cancellationToken);
        return ToProjectDto(project);

    }


    public async Task<IReadOnlyCollection<ProjectDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceRoleAsync(workspaceId, currentUserId, [WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member], cancellationToken);

        List<Project> projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.WorkspaceId == workspaceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

        return projects.Select(ToProjectDto).ToArray();
    }


    public async Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Project project = await GetProjectForWorkspaceMemberAsync(projectId, currentUserId, cancellationToken);

        await EnsureWorkspaceRoleAsync(project.WorkspaceId, currentUserId, [WorkspaceRole.Owner, WorkspaceRole.Admin], cancellationToken);


        string normalizedName = request.Name.Trim();

        bool projectNameExists = await _dbContext.Projects
            .AnyAsync(existingProject =>
                existingProject.WorkspaceId == project.WorkspaceId &&
                existingProject.Id != projectId &&
                existingProject.Name == normalizedName,
                cancellationToken);

        if (projectNameExists)
        {
            throw new ConflictException("Project name already exists.");
        }

        project.Name = normalizedName;
        project.Description = request.Description?.Trim();
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToProjectDto(project);


    }


    private async Task EnsureWorkspaceRoleAsync(Guid workspaceId, Guid currentUserId, WorkspaceRole[] allowedRoles, CancellationToken cancellationToken)
    {
        bool hasRole = await _dbContext.WorkspaceMembers
            .AnyAsync(member =>
                member.WorkspaceId == workspaceId &&
                member.UserId == currentUserId &&
                allowedRoles.Contains(member.Role),
                cancellationToken);

        if (!hasRole)
        {
            throw new ForbiddenAccessException();
        }
    }


    private async Task<Project> GetProjectForWorkspaceMemberAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken)
    {
        Project project = await _dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), projectId);

        await EnsureWorkspaceRoleAsync(project.WorkspaceId, currentUserId, [WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member], cancellationToken);

        return project;
    }


    private static ProjectDto ToProjectDto(Project project)
    {
        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.WorkspaceId,
            project.CreatedAt);
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


