using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly DevTrackDbContext _db;
    private readonly IActivityLogService _activityLogService;

    public TaskService(DevTrackDbContext db, IActivityLogService activityLogService)
    {
        _db = db;
        _activityLogService = activityLogService;
    }

    public async Task<TaskResponse> CreateAsync(int userId, UserRole userRole, CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Task title is required.");

        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == request.ProjectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        if (project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot create tasks in an archived project.");

        if (!await HasProjectAccessAsync(project.Id, project.Team.OwnerId, userId, userRole))
            throw new UnauthorizedAccessException("You do not have access to this project.");

        var priorityText = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority;
        if (!Enum.TryParse<TaskPriority>(priorityText, ignoreCase: true, out var parsedPriority))
            throw new InvalidOperationException($"'{request.Priority}' is not a valid priority.");

        int? assigneeId = request.AssigneeId;
        if (assigneeId.HasValue)
        {
            var assigneeIsMember = await _db.ProjectMembers
                .AnyAsync(m => m.ProjectId == project.Id && m.UserId == assigneeId.Value);
            if (!assigneeIsMember)
                throw new InvalidOperationException("Assignee must be a member of this project.");

            if (assigneeId.Value != userId
                && !await CanAssignOthersAsync(project.Id, project.Team.OwnerId, userId, userRole))
                throw new UnauthorizedAccessException("Only a project manager, team owner, or admin can assign tasks to other users.");
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            ProjectId = project.Id,
            Priority = parsedPriority,
            Status = TaskItemStatus.Backlog,
            DueDate = request.DueDate,
            CreatorId = userId,
            AssigneeId = assigneeId
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await _db.Entry(task).Reference(t => t.Creator).LoadAsync();
        if (assigneeId.HasValue)
            await _db.Entry(task).Reference(t => t.Assignee).LoadAsync();

        await _activityLogService.LogAsync(userId, project.Id, "Created task", "Task", task.Id, task.Title);

        return ToResponse(task, project.Name);
    }

    public async Task<IEnumerable<TaskResponse>> GetAllForUserAsync(int userId, UserRole userRole)
    {
        IQueryable<TaskItem> query = _db.TaskItems
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.Creator)
            .Include(t => t.Assignee);

        if (userRole != UserRole.Admin)
        {
            query = query.Where(t =>
                t.Project.Team.OwnerId == userId ||
                t.Project.Members.Any(m => m.UserId == userId));
        }

        var tasks = await query.ToListAsync();
        return tasks.Select(t => ToResponse(t, t.Project.Name));
    }

    public async Task<TaskResponse?> GetByIdAsync(int taskId, int userId, UserRole userRole)
    {
        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .Include(t => t.Project).ThenInclude(p => p.Members)
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null) return null;

        var isAuthorized = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || task.Project.Members.Any(m => m.UserId == userId);

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You do not have access to this task.");

        return ToResponse(task, task.Project.Name);
    }

    private async Task<bool> HasProjectAccessAsync(int projectId, int teamOwnerId, int userId, UserRole userRole)
    {
        if (userRole == UserRole.Admin) return true;
        if (teamOwnerId == userId) return true;
        return await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
    }

    private async Task<bool> CanAssignOthersAsync(int projectId, int teamOwnerId, int userId, UserRole userRole)
    {
        if (userRole == UserRole.Admin) return true;
        if (teamOwnerId == userId) return true;
        if (userRole == UserRole.ProjectManager)
            return await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
        return false;
    }

    private static TaskResponse ToResponse(TaskItem task, string projectName)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            AssigneeId = task.AssigneeId,
            AssigneeName = task.Assignee?.FullName,
            CreatorId = task.CreatorId,
            CreatorName = task.Creator?.FullName ?? string.Empty
        };
    }
    public async Task<TaskResponse> UpdateAsync(int taskId, int userId, UserRole userRole, UpdateTaskRequest request)
    {
        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (task.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify tasks in an archived project.");

        var isAuthorized = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || task.CreatorId == userId;

        if (!isAuthorized)
            throw new UnauthorizedAccessException("Only the team owner, admin, or task creator can update this task.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Task title is required.");

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        await _db.SaveChangesAsync();

        return ToResponse(task, task.Project.Name);
    }

    public async Task<TaskResponse> UpdateStatusAsync(int taskId, int userId, UserRole userRole, string newStatus)
    {
        if (!Enum.TryParse<TaskItemStatus>(newStatus, ignoreCase: true, out var parsedStatus))
            throw new InvalidOperationException($"'{newStatus}' is not a valid task status.");

        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (task.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify tasks in an archived project.");

        var isAuthorized = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || task.AssigneeId == userId
            || (userRole == UserRole.ProjectManager
                && await _db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == userId));

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You are not allowed to change this task's status.");

        var oldStatus = task.Status;
        task.Status = parsedStatus;
        await _db.SaveChangesAsync();

        await _activityLogService.LogAsync(userId, task.ProjectId, "Changed task status", "Task", task.Id, $"{oldStatus} \u2192 {parsedStatus}");

        return ToResponse(task, task.Project.Name);
    }
    public async Task<TaskResponse> UpdateAssigneeAsync(int taskId, int userId, UserRole userRole, int? newAssigneeId)
    {
        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (task.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify tasks in an archived project.");

        var hasAccess = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || await _db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == userId);

        if (!hasAccess)
            throw new UnauthorizedAccessException("You do not have access to this task.");

        if (newAssigneeId.HasValue)
        {
            var assigneeIsMember = await _db.ProjectMembers
                .AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == newAssigneeId.Value);
            if (!assigneeIsMember)
                throw new InvalidOperationException("Assignee must be a member of this project.");

            if (newAssigneeId.Value != userId
                && !await CanAssignOthersAsync(task.ProjectId, task.Project.Team.OwnerId, userId, userRole))
                throw new UnauthorizedAccessException("Only a project manager, team owner, or admin can assign tasks to other users.");
        }

        task.AssigneeId = newAssigneeId;
        await _db.SaveChangesAsync();

        await _db.Entry(task).Reference(t => t.Assignee).LoadAsync();

        var assigneeDetail = task.Assignee?.FullName ?? "Unassigned";
        await _activityLogService.LogAsync(userId, task.ProjectId, "Assigned task", "Task", task.Id, assigneeDetail);

        return ToResponse(task, task.Project.Name);
    }

    public async Task<TaskResponse> UpdatePriorityAsync(int taskId, int userId, UserRole userRole, string newPriority)
    {
        if (!Enum.TryParse<TaskPriority>(newPriority, ignoreCase: true, out var parsedPriority))
            throw new InvalidOperationException($"'{newPriority}' is not a valid priority.");

        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (task.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify tasks in an archived project.");

        var isAuthorized = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || task.CreatorId == userId;

        if (!isAuthorized)
            throw new UnauthorizedAccessException("Only the team owner, admin, or task creator can change this task's priority.");

        task.Priority = parsedPriority;
        await _db.SaveChangesAsync();

        return ToResponse(task, task.Project.Name);
    }

    public async Task DeleteAsync(int taskId, int userId, UserRole userRole)
    {
        var task = await _db.TaskItems.Include(t => t.Project).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (userRole != UserRole.Admin && task.Project.Team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can delete this task.");

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();
    }
}