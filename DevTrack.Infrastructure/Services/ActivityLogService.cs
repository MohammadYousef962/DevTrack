using DevTrack.Application.DTOs.ActivityLogs;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly DevTrackDbContext _db;

    public ActivityLogService(DevTrackDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int userId, int? projectId, string action, string entityType, int entityId, string? details = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            ProjectId = projectId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedActivityLogResponse> GetForProjectAsync(int projectId, int userId, UserRole userRole, int page, int pageSize)
    {
        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        var hasAccess = userRole == UserRole.Admin
            || project.Team.OwnerId == userId
            || await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);

        if (!hasAccess)
            throw new UnauthorizedAccessException("You do not have access to this project's activity.");

        var query = _db.ActivityLogs.AsNoTracking().Where(a => a.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityLogResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.FullName,
                ProjectId = a.ProjectId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return new PagedActivityLogResponse
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}