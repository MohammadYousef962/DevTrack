using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Teams;

public class CreateTeamRequest
{
    [Required(ErrorMessage = "Team name is required.")]
    [MaxLength(150, ErrorMessage = "Team name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }
}