using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Tasks;

public class UpdateTaskRequest
{
    [Required(ErrorMessage = "Task title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Description cannot exceed 4000 characters.")]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }
}