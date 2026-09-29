using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DevTrack.Application.Common.Exceptions;

namespace DevTrack.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly DevTrackDbContext _db;
    private readonly IActivityLogService _activityLogService;

    public ProjectService(DevTrackDbContext db, IActivityLogService activityLogService)
    {
        _db = db;
        _activityLogService = activityLogService;
    }

    public async Task<ProjectResponse> CreateAsync(int userId, UserRole userRole, CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Project name is required.");

        if (request.DueDate.HasValue && request.DueDate.Value < request.StartDate)
            throw new InvalidOperationException("Due date cannot be before start date.");

        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId);
        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        if (userRole != UserRole.Admin && team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can create projects for this team.");

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            TeamId = request.TeamId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            Status = ProjectStatus.Planning
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        await _db.SaveChangesAsync();

        await _activityLogService.LogAsync(userId, project.Id, "Created project", "Project", project.Id, project.Name);

        return ToResponse(project, team.Name);
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllForUserAsync(int userId, UserRole userRole)
    {
        IQueryable<Project> query = _db.Projects.AsNoTracking().Include(p => p.Team);

        if (userRole != UserRole.Admin)
        {
            query = query.Where(p =>
                p.Team.OwnerId == userId || p.Team.Members.Any(m => m.UserId == userId));
        }

        var projects = await query.ToListAsync();
        return projects.Select(p => ToResponse(p, p.Team.Name));
    }

    public async Task<ProjectResponse?> GetByIdAsync(int projectId, int userId, UserRole userRole)
    {
        var project = await _db.Projects.Include(p => p.Team).ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null) return null;

        var isAuthorized = userRole == UserRole.Admin
            || project.Team.OwnerId == userId
            || project.Team.Members.Any(m => m.UserId == userId);

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You do not have access to this project.");

        return ToResponse(project, project.Team.Name);
    }

    private static ProjectResponse ToResponse(Project project, string teamName)
    {
        return new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            StartDate = project.StartDate,
            DueDate = project.DueDate,
            TeamId = project.TeamId,
            TeamName = teamName,
            CreatedAt = project.CreatedAt
        };
    }
    public async Task<ProjectResponse> UpdateAsync(int projectId, int userId, UserRole userRole, UpdateProjectRequest request)
    {
        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        if (userRole != UserRole.Admin && project.Team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can update this project.");

        if (project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Archived projects cannot be modified.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Project name is required.");

        if (request.DueDate.HasValue && request.DueDate.Value < request.StartDate)
            throw new InvalidOperationException("Due date cannot be before start date.");

        project.Name = request.Name;
        project.Description = request.Description;
        project.StartDate = request.StartDate;
        project.DueDate = request.DueDate;
        await _db.SaveChangesAsync();

        return ToResponse(project, project.Team.Name);
    }

    public async Task<ProjectResponse> UpdateStatusAsync(int projectId, int userId, UserRole userRole, string newStatus)
    {
        if (!Enum.TryParse<ProjectStatus>(newStatus, ignoreCase: true, out var parsedStatus))
            throw new InvalidOperationException($"'{newStatus}' is not a valid project status.");

        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        if (userRole != UserRole.Admin && project.Team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can change this project's status.");

        project.Status = parsedStatus;
        await _db.SaveChangesAsync();

        return ToResponse(project, project.Team.Name);
    }
    public async Task<ProjectMemberResponse> AddMemberAsync(int projectId, int requestingUserId, UserRole requestingUserRole, int newMemberUserId)
    {
        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        if (requestingUserRole != UserRole.Admin && project.Team.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can add project members.");

        if (project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Archived projects cannot be modified.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == newMemberUserId);
        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var isTeamMember = await _db.TeamMembers
            .AnyAsync(m => m.TeamId == project.TeamId && m.UserId == newMemberUserId);
        if (!isTeamMember)
            throw new InvalidOperationException("User must be a member of the project's team first.");

        var alreadyMember = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == newMemberUserId);
        if (alreadyMember)
            throw new ConflictException("User is already a member of this project.");

        var member = new ProjectMember { ProjectId = projectId, UserId = newMemberUserId };
        _db.ProjectMembers.Add(member);
        await _db.SaveChangesAsync();

        return new ProjectMemberResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            JoinedAt = member.JoinedAt
        };
    }

    public async Task RemoveMemberAsync(int projectId, int requestingUserId, UserRole requestingUserRole, int memberUserId)
    {
        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        if (requestingUserRole != UserRole.Admin && project.Team.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can remove project members.");

        if (project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Archived projects cannot be modified.");

        var membership = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberUserId);
        if (membership is null)
            throw new KeyNotFoundException("This user is not a member of this project.");

        _db.ProjectMembers.Remove(membership);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProjectMemberResponse>> GetMembersAsync(int projectId, int requestingUserId, UserRole requestingUserRole)
    {
        var project = await _db.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        var isTeamMember = await _db.TeamMembers
            .AnyAsync(m => m.TeamId == project.TeamId && m.UserId == requestingUserId);

        var isAuthorized = requestingUserRole == UserRole.Admin
            || project.Team.OwnerId == requestingUserId
            || isTeamMember;

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You do not have access to this project.");

        return await _db.ProjectMembers
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .Select(m => new ProjectMemberResponse
            {
                UserId = m.UserId,
                FullName = m.User.FullName,
                Email = m.User.Email,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();
    }
}