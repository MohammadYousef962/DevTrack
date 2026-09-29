using DevTrack.Application.DTOs.Comments;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly DevTrackDbContext _db;

    public CommentService(DevTrackDbContext db)
    {
        _db = db;
    }

    public async Task<CommentResponse> CreateAsync(int taskId, int userId, UserRole userRole, CreateCommentRequest request)
    {
        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        if (task.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot comment on tasks in an archived project.");

        var hasAccess = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || await _db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == userId);

        if (!hasAccess)
            throw new UnauthorizedAccessException("You do not have access to this task.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new InvalidOperationException("Comment content is required.");

        if (request.Content.Length > 2000)
            throw new InvalidOperationException("Comment cannot exceed 2000 characters.");

        var comment = new Comment
        {
            Content = request.Content,
            TaskItemId = taskId,
            AuthorId = userId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        await _db.Entry(comment).Reference(c => c.Author).LoadAsync();

        return ToResponse(comment);
    }

    public async Task<IEnumerable<CommentResponse>> GetAllForTaskAsync(int taskId, int userId, UserRole userRole)
    {
        var task = await _db.TaskItems
            .Include(t => t.Project).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new KeyNotFoundException("Task not found.");

        var hasAccess = userRole == UserRole.Admin
            || task.Project.Team.OwnerId == userId
            || await _db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == userId);

        if (!hasAccess)
            throw new UnauthorizedAccessException("You do not have access to this task.");

        return await _db.Comments
            .AsNoTracking()
            .Where(c => c.TaskItemId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                Content = c.Content,
                TaskItemId = c.TaskItemId,
                AuthorId = c.AuthorId,
                AuthorName = c.Author.FullName,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    private static CommentResponse ToResponse(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            Content = comment.Content,
            TaskItemId = comment.TaskItemId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.Author?.FullName ?? string.Empty,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
    public async Task<CommentResponse> UpdateAsync(int commentId, int userId, UserRole userRole, UpdateCommentRequest request)
    {
        var comment = await _db.Comments
            .Include(c => c.Author)
            .Include(c => c.TaskItem).ThenInclude(t => t.Project)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null)
            throw new KeyNotFoundException("Comment not found.");

        if (comment.TaskItem.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify comments in an archived project.");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Only the comment's author can edit it.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new InvalidOperationException("Comment content is required.");

        if (request.Content.Length > 2000)
            throw new InvalidOperationException("Comment cannot exceed 2000 characters.");

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToResponse(comment);
    }

    public async Task DeleteAsync(int commentId, int userId, UserRole userRole)
    {
        var comment = await _db.Comments
            .Include(c => c.TaskItem).ThenInclude(t => t.Project).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null)
            throw new KeyNotFoundException("Comment not found.");

        if (comment.TaskItem.Project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Cannot modify comments in an archived project.");

        var isAuthorized = comment.AuthorId == userId
            || userRole == UserRole.Admin
            || comment.TaskItem.Project.Team.OwnerId == userId;

        if (!isAuthorized)
            throw new UnauthorizedAccessException("Only the comment's author, team owner, or an admin can delete it.");

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
    }
}