using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Projects;

public class UpdateProjectRequest
{
    [Required(ErrorMessage = "Project name is required.")]
    [MaxLength(150, ErrorMessage = "Project name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateTime StartDate { get; set; }

    public DateTime? DueDate { get; set; }
}