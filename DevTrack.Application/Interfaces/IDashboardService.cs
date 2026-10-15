using DevTrack.Application.Common.Models;
using DevTrack.Application.DTOs.ActivityLogs;
using DevTrack.Application.DTOs.Dashboard;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetStatsAsync(int userId, UserRole role, DateOnly today);
    Task<PagedResult<RecentActivityResponse>> GetRecentActivityAsync(int userId, UserRole role, int page, int pageSize);
}