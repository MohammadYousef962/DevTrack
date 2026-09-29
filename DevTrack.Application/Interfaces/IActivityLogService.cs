using DevTrack.Application.DTOs.ActivityLogs;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(int userId, int? projectId, string action, string entityType, int entityId, string? details = null);
    Task<PagedActivityLogResponse> GetForProjectAsync(int projectId, int userId, UserRole userRole, int page, int pageSize);
}