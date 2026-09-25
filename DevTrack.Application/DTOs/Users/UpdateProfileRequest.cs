namespace DevTrack.Application.DTOs.Users;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}