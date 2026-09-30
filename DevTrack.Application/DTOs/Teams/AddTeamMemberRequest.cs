using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Teams;

public class AddTeamMemberRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid user id is required.")]
    public int UserId { get; set; }
}