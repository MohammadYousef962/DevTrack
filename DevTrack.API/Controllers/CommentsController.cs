using System.Security.Claims;
using DevTrack.Application.DTOs.Comments;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost("api/tasks/{taskId:int}/comments")]
    public async Task<IActionResult> Create(int taskId, CreateCommentRequest request)
    {
        var comment = await _commentService.CreateAsync(taskId, GetUserId(), GetUserRole(), request);
        return StatusCode(201, comment);
    }

    [HttpGet("api/tasks/{taskId:int}/comments")]
    public async Task<IActionResult> GetAllForTask(int taskId)
    {
        var comments = await _commentService.GetAllForTaskAsync(taskId, GetUserId(), GetUserRole());
        return Ok(comments);
    }

    [HttpPut("api/comments/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCommentRequest request)
    {
        var comment = await _commentService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
        return Ok(comment);
    }

    [HttpDelete("api/comments/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _commentService.DeleteAsync(id, GetUserId(), GetUserRole());
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}