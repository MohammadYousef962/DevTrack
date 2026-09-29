namespace DevTrack.Application.DTOs.Projects;

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}