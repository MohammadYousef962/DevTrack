namespace DevTrack.Application.DTOs.Dashboard;

public class DashboardStatsResponse
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalTeams { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int MyOpenTasks { get; set; }
    public int MyTasksDueSoon { get; set; }
    public List<DashboardStatusCount> TasksByStatus { get; set; } = new();
    public List<DashboardUpcomingTask> UpcomingTasks { get; set; } = new();
    public List<DashboardProjectProgress> ProjectProgress { get; set; } = new();

    // Only filled in for admins; null for everyone else.
    public int? TotalUsers { get; set; }
    public int? ActiveUsers { get; set; }
}

public class DashboardStatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardUpcomingTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string? AssigneeName { get; set; }
}

public class DashboardProjectProgress
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int ProgressPercent { get; set; }
}