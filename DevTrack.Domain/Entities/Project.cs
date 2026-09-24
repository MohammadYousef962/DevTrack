using DevTrack.Domain.Enums;

namespace DevTrack.Domain.Entities;

public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}