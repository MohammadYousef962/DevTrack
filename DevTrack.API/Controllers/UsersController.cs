using System.Security.Claims;
using DevTrack.Application.DTOs.Users;
using DevTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevTrack.Application.Common;

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
    [HttpPut("me/avatar")]
    public async Task<IActionResult> UploadAvatar(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Read at most one byte past the limit, so an oversized upload is rejected without buffering all of it.
        var buffer = new byte[AvatarRules.MaxBytes + 1];
        var total = 0;
        int read;
        while (total < buffer.Length &&
               (read = await Request.Body.ReadAsync(buffer.AsMemory(total), cancellationToken)) > 0)
        {
            total += read;
        }

        var updated = await _userService.SetAvatarAsync(userId, buffer[..total]);
        return Ok(updated);
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> RemoveAvatar()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var updated = await _userService.RemoveAvatarAsync(userId);
        return Ok(updated);
    }

    [HttpGet("{id:int}/avatar")]
    public async Task<IActionResult> GetAvatar(int id)
    {
        var avatar = await _userService.GetAvatarAsync(id);
        if (avatar is null)
            throw new KeyNotFoundException("This user has no profile picture.");

        // The address carries a version (?v=...), so a given address never changes and can be cached.
        Response.Headers.CacheControl = "private, max-age=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(avatar.Data, avatar.ContentType);
    }
}