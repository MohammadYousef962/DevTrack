using DevTrack.Domain.Enums;

namespace DevTrack.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Backlog;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}