using System.Security.Claims;
using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        try
        {
            var task = await _taskService.CreateAsync(GetUserId(), GetUserRole(), request);
            return StatusCode(201, task);
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
        var tasks = await _taskService.GetAllForUserAsync(GetUserId(), GetUserRole());
        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var task = await _taskService.GetByIdAsync(id, GetUserId(), GetUserRole());
            if (task is null) return NotFound();
            return Ok(task);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request)
    {
        try
        {
            var task = await _taskService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
            return Ok(task);
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
    public async Task<IActionResult> UpdateStatus(int id, UpdateTaskStatusRequest request)
    {
        try
        {
            var task = await _taskService.UpdateStatusAsync(id, GetUserId(), GetUserRole(), request.Status);
            return Ok(task);
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

    [HttpPatch("{id:int}/assignee")]
    public async Task<IActionResult> UpdateAssignee(int id, UpdateTaskAssigneeRequest request)
    {
        try
        {
            var task = await _taskService.UpdateAssigneeAsync(id, GetUserId(), GetUserRole(), request.AssigneeId);
            return Ok(task);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id:int}/priority")]
    public async Task<IActionResult> UpdatePriority(int id, UpdateTaskPriorityRequest request)
    {
        try
        {
            var task = await _taskService.UpdatePriorityAsync(id, GetUserId(), GetUserRole(), request.Priority);
            return Ok(task);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _taskService.DeleteAsync(id, GetUserId(), GetUserRole());
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}