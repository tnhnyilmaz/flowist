using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Exceptions;
using Flowist.TaskService.Data;
using Flowist.TaskService.DTOs;
using Flowist.TaskService.Entities;

using MassTransit.Contracts.JobService;

using Microsoft.EntityFrameworkCore;

namespace Flowist.TaskService.Services;

public sealed class TaskItemService : ITaskItemService
{

    private readonly TaskServiceDbContext _dbContext;

    public TaskItemService(TaskServiceDbContext dbContext)
    {
        _dbContext=dbContext;
    }
    public async Task<TaskItemDto> CreateAsync(Guid projectId, CreateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Project project = await GetProjectForWorkspaceRoleAsync(
            projectId,
            currentUserId,
            [WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member],
            cancellationToken
        );

        if (request.AssigneeId.HasValue) await EnsureWorkspaceMemberAsync(project.WorkspaceId, request.AssigneeId.Value, cancellationToken);

        TaskItem task = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = Flowist.Shared.Enums.TaskStatus.Todo,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            CreatedAt = DateTimeOffset.UtcNow,
            DueDate = request.DueDate,
            CreatedBy = currentUserId
        };

        _dbContext.TaskItems.Add(task);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToTaskItemDto(task);

    }

    public async Task<TaskItemDto> AssignAsync(Guid taskId, AssignTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        TaskItem task = await GetTaskForWorkspaceRoleAsync(
             taskId,
             currentUserId,
             [WorkspaceRole.Owner, WorkspaceRole.Admin],
             cancellationToken);

        await EnsureWorkspaceMemberAsync(task.Project.WorkspaceId, request.AssigneeId, cancellationToken);

        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        return ToTaskItemDto(task);

    }


    public async Task DeleteAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        TaskItem task = await GetTaskForWorkspaceRoleAsync(
          taskId,
          currentUserId,
          [WorkspaceRole.Owner, WorkspaceRole.Admin],
          cancellationToken);

        _dbContext.TaskItems.Remove(task);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskItemDto> GetByIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        TaskItem task = await GetTaskForWorkspaceRoleAsync(taskId, currentUserId, [WorkspaceRole.Admin, WorkspaceRole.Member, WorkspaceRole.Owner], cancellationToken);

        return ToTaskItemDto(task);
    }

    public async Task<PagedResult<TaskItemDto>> GetProjectTasksAsync(Guid projectId, TaskFilterRequest filter, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Project project = await GetProjectForWorkspaceRoleAsync(projectId, currentUserId,
        [
            WorkspaceRole.Owner,WorkspaceRole.Admin,WorkspaceRole.Member
        ],
        cancellationToken
        );

        IQueryable<TaskItem> query = _dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId);

        query = ApplyFiltering(query, filter);
        query = ApplyFiltering(query, filter);

        int page = Math.Max(filter.Page, 1);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        int totalCount = await query.CountAsync(cancellationToken);

        List<TaskItem> tasks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskItemDto>(
            tasks.Select(ToTaskItemDto).ToArray(),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<TaskItemDto> UpdateAsync(Guid taskId, UpdateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        TaskItem task = await GetTaskForWorkspaceRoleAsync(taskId, currentUserId, [WorkspaceRole.Admin, WorkspaceRole.Owner, WorkspaceRole.Member], cancellationToken);
        if (request.AssigneeId.HasValue) await EnsureWorkspaceMemberAsync(task.Project.WorkspaceId, request.AssigneeId.Value, cancellationToken);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToTaskItemDto(task);
    }

    public async Task<TaskItemDto> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        TaskItem task = await GetTaskForWorkspaceRoleAsync(
           taskId,
           currentUserId,
           [WorkspaceRole.Owner, WorkspaceRole.Admin, WorkspaceRole.Member],
           cancellationToken);

        task.Status=request.Status;
        task.UpdatedAt=DateTimeOffset.UtcNow;

        return ToTaskItemDto(task);
    }


    private async Task<TaskItem> GetTaskForWorkspaceRoleAsync(Guid taskId, Guid currentUserId, WorkspaceRole[] allowedRoles, CancellationToken cancellationToken)
    {
        TaskItem task = await _dbContext.TaskItems
            .Include(task => task.Project)
            .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), taskId);

        await EnsureWorkspaceRoleAsync(task.Project.WorkspaceId, currentUserId, allowedRoles, cancellationToken);

        return task;
    }
    private async Task<Project> GetProjectForWorkspaceRoleAsync(Guid projectId, Guid currentUserId, WorkspaceRole[] allowedRoles, CancellationToken cancellationToken)
    {
        Project project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), projectId);

        await EnsureWorkspaceRoleAsync(project.WorkspaceId, currentUserId, allowedRoles, cancellationToken);
        return project;
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
    private async Task EnsureWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        bool isMember = await _dbContext.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken);

        if (!isMember)
        {
            throw new BusinessRuleException("Assignee must be a workspace member.");
        }
    }
    private static TaskItemDto ToTaskItemDto(TaskItem task)
    {
        return new TaskItemDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.ProjectId,
            task.DueDate);
    }
    private static IQueryable<TaskItem> ApplyFiltering(IQueryable<TaskItem> query, TaskFilterRequest filter)
    {
        if (filter.Status.HasValue)
        {
            query = query.Where(task => task.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(task => task.Priority == filter.Priority.Value);
        }

        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(task => task.AssigneeId == filter.AssigneeId.Value);
        }

        if (filter.DueFrom.HasValue)
        {
            query = query.Where(task => task.DueDate >= filter.DueFrom.Value);
        }

        if (filter.DueTo.HasValue)
        {
            query = query.Where(task => task.DueDate <= filter.DueTo.Value);
        }

        return query;
    }

}