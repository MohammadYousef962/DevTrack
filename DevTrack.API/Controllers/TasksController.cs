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
        var task = await _taskService.CreateAsync(GetUserId(), GetUserRole(), request);
        return StatusCode(201, task);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TaskQueryParameters parameters)
    {
        var result = await _taskService.GetAllForUserAsync(GetUserId(), GetUserRole(), parameters);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id, GetUserId(), GetUserRole());
        if (task is null) throw new KeyNotFoundException("Task not found.");
        return Ok(task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request)
    {
        var task = await _taskService.UpdateAsync(id, GetUserId(), GetUserRole(), request);
        return Ok(task);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateTaskStatusRequest request)
    {
        var task = await _taskService.UpdateStatusAsync(id, GetUserId(), GetUserRole(), request.Status);
        return Ok(task);
    }

    [HttpPatch("{id:int}/assignee")]
    public async Task<IActionResult> UpdateAssignee(int id, UpdateTaskAssigneeRequest request)
    {
        var task = await _taskService.UpdateAssigneeAsync(id, GetUserId(), GetUserRole(), request.AssigneeId);
        return Ok(task);
    }

    [HttpPatch("{id:int}/priority")]
    public async Task<IActionResult> UpdatePriority(int id, UpdateTaskPriorityRequest request)
    {
        var task = await _taskService.UpdatePriorityAsync(id, GetUserId(), GetUserRole(), request.Priority);
        return Ok(task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _taskService.DeleteAsync(id, GetUserId(), GetUserRole());
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetUserRole() => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}