namespace DevTrack.Application.DTOs.Tasks;

public class TaskQueryParameters
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? AssigneeId { get; set; }
    public int? ProjectId { get; set; }
    public string? Search { get; set; }
    public DateTime? DueBefore { get; set; }
    public DateTime? DueAfter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}