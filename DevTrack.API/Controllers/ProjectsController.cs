using System.Security.Claims;
using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevTrack.Application.Common.Exceptions;
using DevTrack.Application.DTOs.ActivityLogs;

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
        try
        {
            var project = await _projectService.CreateAsync(GetUserId(), GetUserRole(), request);
            return StatusCode(201, project);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        try
        {
            var project = await _projectService.GetByIdAsync(id, GetUserId(), GetUserRole());
            if (project is null) return NotFound();
            return Ok(project);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request)
    {
        try
        {
            var project = await _projectService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateProjectStatusRequest request)
    {
        try
        {
            var project = await _projectService.UpdateStatusAsync(id, GetUserId(), GetUserRole(), request.Status);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, AddProjectMemberRequest request)
    {
        try
        {
            var member = await _projectService.AddMemberAsync(id, GetUserId(), GetUserRole(), request.UserId);
            return StatusCode(201, member);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        try
        {
            await _projectService.RemoveMemberAsync(id, GetUserId(), GetUserRole(), userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        try
        {
            var members = await _projectService.GetMembersAsync(id, GetUserId(), GetUserRole());
            return Ok(members);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
    [HttpGet("{id:int}/activity")]
    public async Task<IActionResult> GetActivity(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _activityLogService.GetForProjectAsync(id, GetUserId(), GetUserRole(), page, pageSize);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}