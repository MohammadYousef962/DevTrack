namespace DevTrack.Application.DTOs.Projects;

public class ProjectMemberResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}