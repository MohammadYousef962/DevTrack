using DevTrack.Application.DTOs.Teams;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly DevTrackDbContext _db;

    public TeamService(DevTrackDbContext db)
    {
        _db = db;
    }

    public async Task<TeamResponse> CreateAsync(int ownerId, CreateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Team name is required.");

        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        // Owner is automatically a member too, so they pass "is a team member" checks later.
        _db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = ownerId });
        await _db.SaveChangesAsync();

        await _db.Entry(team).Reference(t => t.Owner).LoadAsync();
        await _db.Entry(team).Collection(t => t.Members).LoadAsync();

        return ToResponse(team);
    }

    public async Task<IEnumerable<TeamResponse>> GetAllForUserAsync(int userId, UserRole userRole)
    {
        IQueryable<Team> query = _db.Teams.AsNoTracking();

        if (userRole != UserRole.Admin)
        {
            query = query.Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId));
        }

        var teams = await query.Include(t => t.Owner).Include(t => t.Members).ToListAsync();
        return teams.Select(ToResponse);
    }

    public async Task<TeamResponse?> GetByIdAsync(int teamId, int userId, UserRole userRole)
    {
        var team = await _db.Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team is null) return null;

        var isAuthorized = userRole == UserRole.Admin
            || team.OwnerId == userId
            || team.Members.Any(m => m.UserId == userId);

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You do not have access to this team.");

        return ToResponse(team);
    }

    private static TeamResponse ToResponse(Team team)
    {
        return new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            OwnerId = team.OwnerId,
            OwnerName = team.Owner?.FullName ?? string.Empty,
            MemberCount = team.Members?.Count ?? 0,
            CreatedAt = team.CreatedAt
        };
    }
    public async Task<TeamResponse> UpdateAsync(int teamId, int userId, UserRole userRole, UpdateTeamRequest request)
    {
        var team = await _db.Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        if (userRole != UserRole.Admin && team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can update this team.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Team name is required.");

        team.Name = request.Name;
        team.Description = request.Description;
        await _db.SaveChangesAsync();

        return ToResponse(team);
    }

    public async Task DeleteAsync(int teamId, int userId, UserRole userRole)
    {
        var team = await _db.Teams
            .Include(t => t.Projects)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        if (userRole != UserRole.Admin && team.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can delete this team.");

        if (team.Projects.Any())
            throw new InvalidOperationException("Cannot delete a team that still has projects.");

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
    }
    public async Task<TeamMemberResponse> AddMemberAsync(int teamId, int requestingUserId, UserRole requestingUserRole, int newMemberUserId)
    {
        var team = await _db.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        if (requestingUserRole != UserRole.Admin && team.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can add members.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == newMemberUserId);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        if (team.Members.Any(m => m.UserId == newMemberUserId))
            throw new InvalidOperationException("User is already a member of this team.");

        var member = new TeamMember { TeamId = teamId, UserId = newMemberUserId };
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync();

        await _db.Entry(member).Reference(m => m.User).LoadAsync();

        return new TeamMemberResponse
        {
            UserId = member.UserId,
            FullName = member.User.FullName,
            Email = member.User.Email,
            JoinedAt = member.JoinedAt
        };
    }

    public async Task RemoveMemberAsync(int teamId, int requestingUserId, UserRole requestingUserRole, int memberUserId)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        if (requestingUserRole != UserRole.Admin && team.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Only the team owner or an admin can remove members.");

        if (memberUserId == team.OwnerId)
            throw new InvalidOperationException("Cannot remove the team owner. Transfer ownership first.");

        var membership = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberUserId);
        if (membership is null)
            throw new KeyNotFoundException("This user is not a member of this team.");

        _db.TeamMembers.Remove(membership);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<TeamMemberResponse>> GetMembersAsync(int teamId, int requestingUserId, UserRole requestingUserRole)
    {
        var team = await _db.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team is null)
            throw new KeyNotFoundException("Team not found.");

        var isAuthorized = requestingUserRole == UserRole.Admin
            || team.OwnerId == requestingUserId
            || team.Members.Any(m => m.UserId == requestingUserId);

        if (!isAuthorized)
            throw new UnauthorizedAccessException("You do not have access to this team.");

        return team.Members.Select(m => new TeamMemberResponse
        {
            UserId = m.UserId,
            FullName = m.User.FullName,
            Email = m.User.Email,
            JoinedAt = m.JoinedAt
        });
    }
}