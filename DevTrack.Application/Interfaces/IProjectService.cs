using DevTrack.Application.DTOs.Projects;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(int userId, UserRole userRole, CreateProjectRequest request);
    Task<IEnumerable<ProjectResponse>> GetAllForUserAsync(int userId, UserRole userRole);
    Task<ProjectResponse?> GetByIdAsync(int projectId, int userId, UserRole userRole);
    Task<ProjectResponse> UpdateAsync(int projectId, int userId, UserRole userRole, UpdateProjectRequest request);
    Task<ProjectResponse> UpdateStatusAsync(int projectId, int userId, UserRole userRole, string newStatus);
    Task<ProjectMemberResponse> AddMemberAsync(int projectId, int requestingUserId, UserRole requestingUserRole, int newMemberUserId);
    Task RemoveMemberAsync(int projectId, int requestingUserId, UserRole requestingUserRole, int memberUserId);
    Task<IEnumerable<ProjectMemberResponse>> GetMembersAsync(int projectId, int requestingUserId, UserRole requestingUserRole);
}