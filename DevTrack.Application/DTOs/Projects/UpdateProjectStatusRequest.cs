using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Projects;

public class UpdateProjectStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}