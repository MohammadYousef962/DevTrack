namespace DevTrack.Domain.Entities;

public class Comment
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}