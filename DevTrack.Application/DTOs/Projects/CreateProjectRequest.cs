namespace DevTrack.Application.DTOs.Projects;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TeamId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}