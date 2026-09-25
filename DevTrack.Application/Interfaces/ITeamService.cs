using DevTrack.Application.DTOs.Teams;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface ITeamService
{
    Task<TeamResponse> CreateAsync(int ownerId, CreateTeamRequest request);
    Task<IEnumerable<TeamResponse>> GetAllForUserAsync(int userId, UserRole userRole);
    Task<TeamResponse?> GetByIdAsync(int teamId, int userId, UserRole userRole);
    Task<TeamResponse> UpdateAsync(int teamId, int userId, UserRole userRole, UpdateTeamRequest request);
    Task DeleteAsync(int teamId, int userId, UserRole userRole);
    Task<TeamMemberResponse> AddMemberAsync(int teamId, int requestingUserId, UserRole requestingUserRole, int newMemberUserId);
    Task RemoveMemberAsync(int teamId, int requestingUserId, UserRole requestingUserRole, int memberUserId);
    Task<IEnumerable<TeamMemberResponse>> GetMembersAsync(int teamId, int requestingUserId, UserRole requestingUserRole);
}