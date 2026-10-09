using System.Security.Claims;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [Authorize(Roles = "Admin,ProjectManager")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTeamRequest request)
    {
        var team = await _teamService.CreateAsync(GetUserId(), request);
        return StatusCode(201, team);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var teams = await _teamService.GetAllForUserAsync(GetUserId(), GetUserRole());
        return Ok(teams);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var team = await _teamService.GetByIdAsync(id, GetUserId(), GetUserRole());
        if (team is null) throw new KeyNotFoundException("Team not found.");
        return Ok(team);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTeamRequest request)
    {
        var team = await _teamService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
        return Ok(team);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _teamService.DeleteAsync(id, GetUserId(), GetUserRole());
        return NoContent();
    }

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, AddTeamMemberRequest request)
    {
        var member = await _teamService.AddMemberAsync(id, GetUserId(), GetUserRole(), request.UserId);
        return StatusCode(201, member);
    }

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await _teamService.RemoveMemberAsync(id, GetUserId(), GetUserRole(), userId);
        return NoContent();
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var members = await _teamService.GetMembersAsync(id, GetUserId(), GetUserRole());
        return Ok(members);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}