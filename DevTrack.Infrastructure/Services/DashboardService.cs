using DevTrack.Application.Common.Models;
using DevTrack.Application.DTOs.ActivityLogs;
using DevTrack.Application.DTOs.Dashboard;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private const int UpcomingLimit = 5;
    private const int ProgressLimit = 6;
    private const int DueSoonDays = 7;
    private const int MaxPageSize = 100;

    private readonly DevTrackDbContext _db;

    public DashboardService(DevTrackDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsResponse> GetStatsAsync(int userId, UserRole role, DateOnly today)
    {
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var dueSoonEnd = todayStart.AddDays(DueSoonDays + 1); // exclusive, so the whole seventh day counts

        var visibleProjects = VisibleProjects(userId, role);
        var visibleTasks = VisibleTasks(userId, role);

        var statusCounts = await visibleTasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var response = new DashboardStatsResponse
        {
            TotalProjects = await visibleProjects.CountAsync(),
            ActiveProjects = await visibleProjects.CountAsync(p => p.Status == ProjectStatus.Active),
            TotalTeams = await VisibleTeamCountAsync(userId, role),
            TasksByStatus = Enum.GetValues<TaskItemStatus>()
                .Select(status => new DashboardStatusCount
                {
                    Status = status.ToString(),
                    Count = statusCounts.Where(c => c.Status == status).Select(c => c.Count).FirstOrDefault()
                })
                .ToList(),
            OverdueTasks = await visibleTasks.CountAsync(t =>
                t.Status != TaskItemStatus.Done && t.DueDate != null && t.DueDate < todayStart),
            MyOpenTasks = await visibleTasks.CountAsync(t =>
                t.AssigneeId == userId && t.Status != TaskItemStatus.Done),
            MyTasksDueSoon = await visibleTasks.CountAsync(t =>
                t.AssigneeId == userId && t.Status != TaskItemStatus.Done &&
                t.DueDate != null && t.DueDate >= todayStart && t.DueDate < dueSoonEnd)
        };

        response.TotalTasks = response.TasksByStatus.Sum(s => s.Count);
        response.CompletedTasks = response.TasksByStatus.First(s => s.Status == TaskItemStatus.Done.ToString()).Count;

        var upcoming = await visibleTasks
            .Where(t => t.Status != TaskItemStatus.Done && t.DueDate != null &&
                        t.DueDate >= todayStart && t.DueDate < dueSoonEnd)
            .OrderBy(t => t.DueDate).ThenBy(t => t.Id)
            .Take(UpcomingLimit)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.ProjectId,
                ProjectName = t.Project.Name,
                t.Priority,
                t.Status,
                t.DueDate,
                AssigneeName = t.Assignee != null ? t.Assignee.FullName : null
            })
            .ToListAsync();

        response.UpcomingTasks = upcoming
            .Select(t => new DashboardUpcomingTask
            {
                Id = t.Id,
                Title = t.Title,
                ProjectId = t.ProjectId,
                ProjectName = t.ProjectName,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                DueDate = t.DueDate!.Value,
                AssigneeName = t.AssigneeName
            })
            .ToList();

        // Projects closest to their deadline first; projects without a due date go last.
        var progressProjects = await ProjectsWithTaskAccess(userId, role)
            .Where(p => p.Status != ProjectStatus.Archived)
            .OrderBy(p => p.DueDate == null).ThenBy(p => p.DueDate).ThenBy(p => p.Id)
            .Take(ProgressLimit)
            .Select(p => new { p.Id, p.Name, p.Status, p.DueDate })
            .ToListAsync();

        var projectIds = progressProjects.Select(p => p.Id).ToList();
        var taskCounts = await visibleTasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .GroupBy(t => t.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Total = g.Count(),
                Done = g.Count(t => t.Status == TaskItemStatus.Done)
            })
            .ToListAsync();

        response.ProjectProgress = progressProjects
            .Select(p =>
            {
                var counts = taskCounts.FirstOrDefault(c => c.ProjectId == p.Id);
                var total = counts?.Total ?? 0;
                var done = counts?.Done ?? 0;
                return new DashboardProjectProgress
                {
                    ProjectId = p.Id,
                    Name = p.Name,
                    Status = p.Status.ToString(),
                    DueDate = p.DueDate,
                    TotalTasks = total,
                    CompletedTasks = done,
                    ProgressPercent = total == 0 ? 0 : done * 100 / total
                };
            })
            .ToList();

        if (role == UserRole.Admin)
        {
            response.TotalUsers = await _db.Users.CountAsync();
            response.ActiveUsers = await _db.Users.CountAsync(u => u.IsActive);
        }

        return response;
    }

    public async Task<PagedResult<RecentActivityResponse>> GetRecentActivityAsync(
        int userId, UserRole role, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        IQueryable<ActivityLog> entries = _db.ActivityLogs.AsNoTracking();

        if (role != UserRole.Admin)
        {
            // Entries of projects the user can open, plus the user's own entries whose project no longer exists.
            var accessibleProjectIds = ProjectsWithTaskAccess(userId, role).Select(p => p.Id);
            entries = entries.Where(a =>
                (a.ProjectId != null && accessibleProjectIds.Contains(a.ProjectId.Value)) ||
                (a.ProjectId == null && a.UserId == userId));
        }

        var totalCount = await entries.CountAsync();

        var rows = await entries
            .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserName = _db.Users.Where(u => u.Id == a.UserId).Select(u => u.FullName).FirstOrDefault(),
                a.ProjectId,
                ProjectName = a.ProjectId == null
                    ? null
                    : _db.Projects.Where(p => p.Id == a.ProjectId).Select(p => p.Name).FirstOrDefault(),
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Details,
                a.Timestamp
            })
            .ToListAsync();

        var items = rows
            .Select(r => new RecentActivityResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.UserName ?? "Unknown user",
                ProjectId = r.ProjectId,
                ProjectName = r.ProjectName,
                Action = r.Action,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                Details = r.Details,
                Timestamp = r.Timestamp
            })
            .ToList();

        return PagedResult.Create<RecentActivityResponse>(items, page, pageSize, totalCount);
    }

    // Same rule as the Projects list: admins see everything, everyone else sees the projects
    // of teams they own or belong to.
    private IQueryable<Project> VisibleProjects(int userId, UserRole role)
    {
        var projects = _db.Projects.AsNoTracking();
        if (role == UserRole.Admin)
            return projects;

        return projects.Where(p =>
            p.Team.OwnerId == userId ||
            _db.TeamMembers.Any(m => m.TeamId == p.TeamId && m.UserId == userId));
    }

    // Same rule as tasks and per-project activity: admins see everything, everyone else needs
    // to be a member of the project or the owner of its team.
    private IQueryable<Project> ProjectsWithTaskAccess(int userId, UserRole role)
    {
        var projects = _db.Projects.AsNoTracking();
        if (role == UserRole.Admin)
            return projects;

        return projects.Where(p =>
            p.Team.OwnerId == userId ||
            _db.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId));
    }

    private IQueryable<TaskItem> VisibleTasks(int userId, UserRole role)
    {
        var accessibleProjectIds = ProjectsWithTaskAccess(userId, role).Select(p => p.Id);
        return _db.TaskItems.AsNoTracking().Where(t => accessibleProjectIds.Contains(t.ProjectId));
    }

    private Task<int> VisibleTeamCountAsync(int userId, UserRole role)
    {
        var teams = _db.Teams.AsNoTracking();
        if (role == UserRole.Admin)
            return teams.CountAsync();

        return teams.CountAsync(t =>
            t.OwnerId == userId || _db.TeamMembers.Any(m => m.TeamId == t.Id && m.UserId == userId));
    }
}