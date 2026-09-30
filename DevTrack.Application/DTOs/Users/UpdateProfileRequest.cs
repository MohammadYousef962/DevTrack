using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Users;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Url(ErrorMessage = "Profile image must be a valid URL.")]
    public string? ProfileImageUrl { get; set; }
}