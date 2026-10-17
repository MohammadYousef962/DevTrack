using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class ProjectServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly Mock<IActivityLogService> _activityLogMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _activityLogMock = new Mock<IActivityLogService>();
        _sut = new ProjectService(_db, _activityLogMock.Object);
    }

    private async Task<(User owner, Team team)> SeedOwnerAndTeamAsync()
    {
        var owner = new User { FullName = "Team Owner", Email = "owner@example.com", PasswordHash = "x", Role = UserRole.ProjectManager };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var team = new Team { Name = "Test Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return (owner, team);
    }

    [Fact]
    public async Task CreateAsync_WithDueDateBeforeStartDate_ThrowsInvalidOperationException()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();

        var request = new CreateProjectRequest
        {
            Name = "Bad Dates",
            TeamId = team.Id,
            StartDate = new DateTime(2026, 6, 1),
            DueDate = new DateTime(2026, 1, 1)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentTeam_ThrowsKeyNotFoundException()
    {
        var request = new CreateProjectRequest
        {
            Name = "Orphan Project",
            TeamId = 9999,
            StartDate = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(1, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task CreateAsync_ByNonOwnerNonAdmin_ThrowsUnauthorizedAccessException()
    {
        var (_, team) = await SeedOwnerAndTeamAsync();

        var outsider = new User { FullName = "Outsider", Email = "outsider@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(outsider);
        await _db.SaveChangesAsync();

        var request = new CreateProjectRequest
        {
            Name = "Unauthorized Attempt",
            TeamId = team.Id,
            StartDate = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(outsider.Id, UserRole.Developer, request));
    }

    [Fact]
    public async Task CreateAsync_ByTeamOwner_CreatesProjectAndAddsOwnerAsProjectMember()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();

        var request = new CreateProjectRequest
        {
            Name = "Real Project",
            TeamId = team.Id,
            StartDate = DateTime.UtcNow
        };

        var result = await _sut.CreateAsync(owner.Id, UserRole.ProjectManager, request);

        Assert.Equal("Planning", result.Status);

        var isMember = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == result.Id && m.UserId == owner.Id);
        Assert.True(isMember);
    }

    [Fact]
    public async Task UpdateAsync_OnArchivedProject_ThrowsInvalidOperationException()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Archived", TeamId = team.Id, StartDate = DateTime.UtcNow, Status = ProjectStatus.Archived };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var request = new UpdateProjectRequest { Name = "Trying to edit", StartDate = DateTime.UtcNow };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(project.Id, owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatusValue_ThrowsInvalidOperationException()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Status Test", TeamId = team.Id, StartDate = DateTime.UtcNow };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(project.Id, owner.Id, UserRole.ProjectManager, "NotAStatus"));
    }

    [Fact]
    public async Task RemoveMemberAsync_UnassignsTheRemovedMembersTasksInThatProject()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var developer = new User { FullName = "Dev", Email = "dev@example.com", PasswordHash = "x", Role = UserRole.Developer };
        var project = new Project { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow };
        _db.Users.Add(developer);
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = developer.Id });
        var devTask = new TaskItem { Title = "Dev's task", ProjectId = project.Id, CreatorId = owner.Id, AssigneeId = developer.Id };
        var ownerTask = new TaskItem { Title = "Owner's task", ProjectId = project.Id, CreatorId = owner.Id, AssigneeId = owner.Id };
        _db.TaskItems.AddRange(devTask, ownerTask);
        await _db.SaveChangesAsync();

        await _sut.RemoveMemberAsync(project.Id, owner.Id, UserRole.ProjectManager, developer.Id);

        var devTaskAfter = await _db.TaskItems.AsNoTracking().FirstAsync(t => t.Id == devTask.Id);
        var ownerTaskAfter = await _db.TaskItems.AsNoTracking().FirstAsync(t => t.Id == ownerTask.Id);
        Assert.Null(devTaskAfter.AssigneeId);
        Assert.Equal(owner.Id, ownerTaskAfter.AssigneeId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProjectTasksCommentsAndMembers_AndDetachesActivityLogs()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Doomed project", TeamId = team.Id, StartDate = DateTime.UtcNow };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = owner.Id });
        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        _db.Comments.Add(new Comment { Content = "A comment", TaskItemId = task.Id, AuthorId = owner.Id });
        _db.ActivityLogs.Add(new ActivityLog { UserId = owner.Id, ProjectId = project.Id, Action = "Created task", EntityType = "Task", EntityId = task.Id });
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(project.Id, owner.Id, UserRole.ProjectManager);

        Assert.Empty(await _db.Projects.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.TaskItems.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.Comments.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.ProjectMembers.AsNoTracking().ToListAsync());

        var log = await _db.ActivityLogs.AsNoTracking().SingleAsync();
        Assert.Null(log.ProjectId);

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, null, "Deleted project", "Project", project.Id, "Doomed project"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ByNonOwnerNonAdmin_ThrowsUnauthorizedAccessException()
    {
        var (_, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Protected", TeamId = team.Id, StartDate = DateTime.UtcNow };
        var outsider = new User { FullName = "Outsider", Email = "outsider@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Projects.Add(project);
        _db.Users.Add(outsider);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(project.Id, outsider.Id, UserRole.Developer));

        Assert.NotNull(await _db.Projects.FindAsync(project.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownProject_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(9999, 1, UserRole.Admin));
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNumericStatus_ThrowsInvalidOperationException()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Status Test", TeamId = team.Id, StartDate = DateTime.UtcNow };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(project.Id, owner.Id, UserRole.ProjectManager, "99"));
    }
    [Fact]
    public async Task UpdateAsync_LogsOnlyTheFieldsThatChanged()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Old name", Description = "Same", TeamId = team.Id, StartDate = new DateTime(2026, 1, 1) };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _sut.UpdateAsync(project.Id, owner.Id, UserRole.ProjectManager,
            new UpdateProjectRequest
            {
                Name = "New name",
                Description = "Same",
                StartDate = new DateTime(2026, 1, 1),
                DueDate = new DateTime(2026, 6, 1)
            });

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Updated project", "Project", project.Id, "name, due date"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNothingChanged_LogsNothing()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Same name", Description = null, TeamId = team.Id, StartDate = new DateTime(2026, 1, 1) };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _sut.UpdateAsync(project.Id, owner.Id, UserRole.ProjectManager,
            new UpdateProjectRequest
            {
                Name = "Same name",
                Description = "",
                StartDate = new DateTime(2026, 1, 1, 9, 30, 0)
            });

        _activityLogMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStatusAsync_LogsTheChange()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow, Status = ProjectStatus.Planning };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _sut.UpdateStatusAsync(project.Id, owner.Id, UserRole.ProjectManager, "Active");

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Changed project status", "Project", project.Id, "Planning \u2192 Active"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithTheSameStatus_LogsNothing()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow, Status = ProjectStatus.Active };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _sut.UpdateStatusAsync(project.Id, owner.Id, UserRole.ProjectManager, "active");

        _activityLogMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMemberAsync_LogsTheNewMembersName()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow };
        var developer = new User { FullName = "Dana Developer", Email = "dana@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Projects.Add(project);
        _db.Users.Add(developer);
        await _db.SaveChangesAsync();

        _db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = developer.Id });
        await _db.SaveChangesAsync();

        await _sut.AddMemberAsync(project.Id, owner.Id, UserRole.ProjectManager, developer.Id);

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Added project member", "Project", project.Id, "Dana Developer"),
            Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_LogsTheRemovedMembersName()
    {
        var (owner, team) = await SeedOwnerAndTeamAsync();
        var project = new Project { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow };
        var developer = new User { FullName = "Dana Developer", Email = "dana@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Projects.Add(project);
        _db.Users.Add(developer);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = developer.Id });
        await _db.SaveChangesAsync();

        await _sut.RemoveMemberAsync(project.Id, owner.Id, UserRole.ProjectManager, developer.Id);

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Removed project member", "Project", project.Id, "Dana Developer"),
            Times.Once);
    }
}