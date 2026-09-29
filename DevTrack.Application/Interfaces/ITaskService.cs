using DevTrack.Application.DTOs.Tasks;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(int userId, UserRole userRole, CreateTaskRequest request);
    Task<IEnumerable<TaskResponse>> GetAllForUserAsync(int userId, UserRole userRole);
    Task<TaskResponse?> GetByIdAsync(int taskId, int userId, UserRole userRole);
    Task<TaskResponse> UpdateAsync(int taskId, int userId, UserRole userRole, UpdateTaskRequest request);
    Task<TaskResponse> UpdateStatusAsync(int taskId, int userId, UserRole userRole, string newStatus);
    Task<TaskResponse> UpdateAssigneeAsync(int taskId, int userId, UserRole userRole, int? newAssigneeId);
    Task<TaskResponse> UpdatePriorityAsync(int taskId, int userId, UserRole userRole, string newPriority);
    Task DeleteAsync(int taskId, int userId, UserRole userRole);
}