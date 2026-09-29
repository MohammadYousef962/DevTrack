using DevTrack.Application.DTOs.Comments;
using DevTrack.Domain.Enums;

namespace DevTrack.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(int taskId, int userId, UserRole userRole, CreateCommentRequest request);
    Task<IEnumerable<CommentResponse>> GetAllForTaskAsync(int taskId, int userId, UserRole userRole);
    Task<CommentResponse> UpdateAsync(int commentId, int userId, UserRole userRole, UpdateCommentRequest request);
    Task DeleteAsync(int commentId, int userId, UserRole userRole);
}