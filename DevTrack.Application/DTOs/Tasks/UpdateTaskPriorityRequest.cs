using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Tasks;

public class UpdateTaskPriorityRequest
{
    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = string.Empty;
}