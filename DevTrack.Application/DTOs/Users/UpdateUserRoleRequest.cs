using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Users;

public class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}