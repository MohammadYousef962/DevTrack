namespace DevTrack.Application.DTOs.ActivityLogs;

public class ActivityLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}