namespace DevTrack.Application.DTOs.ActivityLogs;

public class PagedActivityLogResponse
{
    public IEnumerable<ActivityLogResponse> Items { get; set; } = new List<ActivityLogResponse>();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}