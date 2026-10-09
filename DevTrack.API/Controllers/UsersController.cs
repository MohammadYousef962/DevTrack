using System.Security.Claims;
using DevTrack.Application.DTOs.Users;
using DevTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) throw new KeyNotFoundException("User not found.");
        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var updated = await _userService.UpdateProfileAsync(userId, request);
        return Ok(updated);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.ChangePasswordAsync(userId, request);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) throw new KeyNotFoundException("User not found.");
        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusRequest request)
    {
        var updated = await _userService.UpdateStatusAsync(id, request.IsActive);
        return Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleRequest request)
    {
        var updated = await _userService.UpdateRoleAsync(id, request.Role);
        return Ok(updated);
    }

    [Authorize(Roles = "Admin,ProjectManager")]
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string email = "")
    {
        var result = await _userService.LookupByEmailAsync(email);
        return Ok(result);
    }
}