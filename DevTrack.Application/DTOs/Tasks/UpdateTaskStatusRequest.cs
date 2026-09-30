using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}