using DevTrack.Application.DTOs.Comments;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;

namespace DevTrack.Tests.UnitTests;

public class CommentServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new CommentService(_db);
    }

    private async Task<(User owner, TaskItem task)> SeedTaskAsync(bool archived = false)
    {
        var owner = new User { FullName = "Owner", Email = "owner@example.com", PasswordHash = "x", Role = UserRole.ProjectManager };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var team = new Team { Name = "Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var project = new Project
        {
            Name = "Project",
            TeamId = team.Id,
            StartDate = DateTime.UtcNow,
            Status = archived ? ProjectStatus.Archived : ProjectStatus.Planning
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        return (owner, task);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyContent_ThrowsInvalidOperationException()
    {
        var (owner, task) = await SeedTaskAsync();
        var request = new CreateCommentRequest { Content = "" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(task.Id, owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task CreateAsync_ByUserWithoutProjectAccess_ThrowsUnauthorizedAccessException()
    {
        var (_, task) = await SeedTaskAsync();
        var outsider = new User { FullName = "Outsider", Email = "outsider@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(outsider);
        await _db.SaveChangesAsync();

        var request = new CreateCommentRequest { Content = "Trying to comment" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(task.Id, outsider.Id, UserRole.Developer, request));
    }

    [Fact]
    public async Task CreateAsync_OnArchivedProject_ThrowsInvalidOperationException()
    {
        var (owner, task) = await SeedTaskAsync(archived: true);
        var request = new CreateCommentRequest { Content = "Too late" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(task.Id, owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task UpdateAsync_ByNonAuthor_ThrowsUnauthorizedAccessException_EvenForAdmin()
    {
        var (owner, task) = await SeedTaskAsync();

        var comment = new Comment { Content = "Original", TaskItemId = task.Id, AuthorId = owner.Id };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var admin = new User { FullName = "Admin", Email = "admin@example.com", PasswordHash = "x", Role = UserRole.Admin };
        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        var request = new UpdateCommentRequest { Content = "Rewritten by admin" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateAsync(comment.Id, admin.Id, UserRole.Admin, request));
    }

    [Fact]
    public async Task DeleteAsync_ByNonAuthorNonModerator_ThrowsUnauthorizedAccessException()
    {
        var (owner, task) = await SeedTaskAsync();

        var comment = new Comment { Content = "Comment", TaskItemId = task.Id, AuthorId = owner.Id };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var outsider = new User { FullName = "Outsider", Email = "outsider@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(outsider);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(comment.Id, outsider.Id, UserRole.Developer));
    }

    [Fact]
    public async Task DeleteAsync_ByTeamOwnerActingAsModerator_Succeeds()
    {
        var (owner, task) = await SeedTaskAsync();

        var author = new User { FullName = "Author", Email = "author@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(author);
        await _db.SaveChangesAsync();

        var comment = new Comment { Content = "Comment", TaskItemId = task.Id, AuthorId = author.Id };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // owner is the team owner, not the comment's author — this is the moderator path
        await _sut.DeleteAsync(comment.Id, owner.Id, UserRole.ProjectManager);

        Assert.Null(await _db.Comments.FindAsync(comment.Id));
    }
}