using System.Security.Claims;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // "today" is the caller's own local date (YYYY-MM-DD), so "overdue" is right in any timezone.
    // Without it the server falls back to the UTC date.
    [HttpGet("api/dashboard/stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateOnly? today)
    {
        var effectiveToday = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var stats = await _dashboardService.GetStatsAsync(GetUserId(), GetUserRole(), effectiveToday);
        return Ok(stats);
    }

    [HttpGet("api/activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _dashboardService.GetRecentActivityAsync(GetUserId(), GetUserRole(), page, pageSize);
        return Ok(result);
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.Parse(value!);
    }

    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}