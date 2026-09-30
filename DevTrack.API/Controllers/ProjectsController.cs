using System.Security.Claims;
using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IActivityLogService _activityLogService;

    public ProjectsController(IProjectService projectService, IActivityLogService activityLogService)
    {
        _projectService = projectService;
        _activityLogService = activityLogService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        var project = await _projectService.CreateAsync(GetUserId(), GetUserRole(), request);
        return StatusCode(201, project);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectService.GetAllForUserAsync(GetUserId(), GetUserRole());
        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id, GetUserId(), GetUserRole());
        if (project is null) throw new KeyNotFoundException("Project not found.");
        return Ok(project);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request)
    {
        var project = await _projectService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
        return Ok(project);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateProjectStatusRequest request)
    {
        var project = await _projectService.UpdateStatusAsync(id, GetUserId(), GetUserRole(), request.Status);
        return Ok(project);
    }

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, AddProjectMemberRequest request)
    {
        var member = await _projectService.AddMemberAsync(id, GetUserId(), GetUserRole(), request.UserId);
        return StatusCode(201, member);
    }

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await _projectService.RemoveMemberAsync(id, GetUserId(), GetUserRole(), userId);
        return NoContent();
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var members = await _projectService.GetMembersAsync(id, GetUserId(), GetUserRole());
        return Ok(members);
    }

    [HttpGet("{id:int}/activity")]
    public async Task<IActionResult> GetActivity(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _activityLogService.GetForProjectAsync(id, GetUserId(), GetUserRole(), page, pageSize);
        return Ok(result);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}