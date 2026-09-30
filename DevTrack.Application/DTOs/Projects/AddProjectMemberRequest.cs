using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Projects;

public class AddProjectMemberRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid user id is required.")]
    public int UserId { get; set; }
}