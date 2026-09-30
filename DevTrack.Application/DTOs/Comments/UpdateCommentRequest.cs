using System.ComponentModel.DataAnnotations;

namespace DevTrack.Application.DTOs.Comments;

public class UpdateCommentRequest
{
    [Required(ErrorMessage = "Comment content is required.")]
    [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters.")]
    public string Content { get; set; } = string.Empty;
}