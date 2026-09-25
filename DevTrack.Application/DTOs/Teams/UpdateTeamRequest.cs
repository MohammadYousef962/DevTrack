namespace DevTrack.Application.DTOs.Teams;

public class UpdateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}