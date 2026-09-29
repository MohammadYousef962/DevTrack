namespace DevTrack.Application.DTOs.Tasks;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProjectId { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? AssigneeId { get; set; }
}