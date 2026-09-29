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
        try
        {
            var comment = await _commentService.CreateAsync(taskId, GetUserId(), GetUserRole(), request);
            return StatusCode(201, comment);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("api/tasks/{taskId:int}/comments")]
    public async Task<IActionResult> GetAllForTask(int taskId)
    {
        try
        {
            var comments = await _commentService.GetAllForTaskAsync(taskId, GetUserId(), GetUserRole());
            return Ok(comments);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("api/comments/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCommentRequest request)
    {
        try
        {
            var comment = await _commentService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
            return Ok(comment);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("api/comments/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _commentService.DeleteAsync(id, GetUserId(), GetUserRole());
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}