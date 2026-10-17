using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class TaskServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly Mock<IActivityLogService> _activityLogMock;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _activityLogMock = new Mock<IActivityLogService>();
        _sut = new TaskService(_db, _activityLogMock.Object);
    }

    private async Task<(User owner, Project project)> SeedProjectAsync(bool archived = false)
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

        return (owner, project);
    }

    private async Task<User> AddProjectMemberAsync(int projectId, UserRole role, string email)
    {
        var user = new User { FullName = email, Email = email, PasswordHash = "x", Role = role };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = user.Id });
        await _db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task CreateAsync_WithAssigneeNotProjectMember_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync();
        var outsider = new User { FullName = "Outsider", Email = "outsider@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(outsider);
        await _db.SaveChangesAsync();

        var request = new CreateTaskRequest { Title = "Task", ProjectId = project.Id, AssigneeId = outsider.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task CreateAsync_ByDeveloperSelfAssigning_Succeeds()
    {
        var (_, project) = await SeedProjectAsync();
        var developer = await AddProjectMemberAsync(project.Id, UserRole.Developer, "dev@example.com");

        var request = new CreateTaskRequest { Title = "Self-assigned", ProjectId = project.Id, AssigneeId = developer.Id };

        var result = await _sut.CreateAsync(developer.Id, UserRole.Developer, request);

        Assert.Equal(developer.Id, result.AssigneeId);
    }

    [Fact]
    public async Task CreateAsync_ByDeveloperAssigningSomeoneElse_ThrowsUnauthorizedAccessException()
    {
        var (_, project) = await SeedProjectAsync();
        var developer = await AddProjectMemberAsync(project.Id, UserRole.Developer, "dev@example.com");
        var otherDeveloper = await AddProjectMemberAsync(project.Id, UserRole.Developer, "other@example.com");

        var request = new CreateTaskRequest { Title = "Reassignment attempt", ProjectId = project.Id, AssigneeId = otherDeveloper.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(developer.Id, UserRole.Developer, request));
    }

    [Fact]
    public async Task CreateAsync_ByProjectManagerAssigningSomeoneElse_Succeeds()
    {
        var (_, project) = await SeedProjectAsync();
        var manager = await AddProjectMemberAsync(project.Id, UserRole.ProjectManager, "pm@example.com");
        var developer = await AddProjectMemberAsync(project.Id, UserRole.Developer, "dev@example.com");

        var request = new CreateTaskRequest { Title = "Assigned by PM", ProjectId = project.Id, AssigneeId = developer.Id };

        var result = await _sut.CreateAsync(manager.Id, UserRole.ProjectManager, request);

        Assert.Equal(developer.Id, result.AssigneeId);
    }

    [Fact]
    public async Task CreateAsync_OnArchivedProject_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync(archived: true);

        var request = new CreateTaskRequest { Title = "Too late", ProjectId = project.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_OnArchivedProject_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync(archived: true);
        var task = new TaskItem { Title = "Frozen", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(task.Id, owner.Id, UserRole.ProjectManager, "InProgress"));
    }

    [Fact]
    public async Task UpdatePriorityAsync_ByAssigneeWhoIsNotCreator_ThrowsUnauthorizedAccessException()
    {
        var (owner, project) = await SeedProjectAsync();
        var assignee = await AddProjectMemberAsync(project.Id, UserRole.Developer, "assignee@example.com");

        var task = new TaskItem { Title = "Not yours to reprioritize", ProjectId = project.Id, CreatorId = owner.Id, AssigneeId = assignee.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdatePriorityAsync(task.Id, assignee.Id, UserRole.Developer, "Critical"));
    }

    [Fact]
    public async Task DeleteAsync_OnArchivedProject_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync(archived: true);
        var task = new TaskItem { Title = "Frozen", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteAsync(task.Id, owner.Id, UserRole.ProjectManager));
    }
    [Fact]
    public async Task CreateAsync_WithNumericPriority_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync();
        var request = new CreateTaskRequest { Title = "Task", ProjectId = project.Id, Priority = "99" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(owner.Id, UserRole.ProjectManager, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNumericStatus_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(task.Id, owner.Id, UserRole.ProjectManager, "99"));
    }

    [Fact]
    public async Task UpdatePriorityAsync_WithNumericPriority_ThrowsInvalidOperationException()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdatePriorityAsync(task.Id, owner.Id, UserRole.ProjectManager, "99"));
    }
    [Fact]
    public async Task UpdateAsync_LogsOnlyTheFieldsThatChanged()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Old title", Description = "Same", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await _sut.UpdateAsync(task.Id, owner.Id, UserRole.ProjectManager,
            new UpdateTaskRequest { Title = "New title", Description = "Same", DueDate = new DateTime(2026, 12, 1) });

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Updated task", "Task", task.Id, "title, due date"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNothingChanged_LogsNothing()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem
        {
            Title = "Same title",
            Description = null,
            DueDate = new DateTime(2026, 12, 1),
            ProjectId = project.Id,
            CreatorId = owner.Id
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        // An empty description counts as none, and a time of day doesn't make it a different due date.
        await _sut.UpdateAsync(task.Id, owner.Id, UserRole.ProjectManager,
            new UpdateTaskRequest { Title = "Same title", Description = "", DueDate = new DateTime(2026, 12, 1, 15, 30, 0) });

        _activityLogMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePriorityAsync_LogsTheChange()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id, Priority = TaskPriority.Medium };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await _sut.UpdatePriorityAsync(task.Id, owner.Id, UserRole.ProjectManager, "Critical");

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Changed task priority", "Task", task.Id, "Medium \u2192 Critical"),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePriorityAsync_WithTheSamePriority_LogsNothing()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Task", ProjectId = project.Id, CreatorId = owner.Id, Priority = TaskPriority.High };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await _sut.UpdatePriorityAsync(task.Id, owner.Id, UserRole.ProjectManager, "high");

        _activityLogMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_LogsTheDeletedTasksTitle()
    {
        var (owner, project) = await SeedProjectAsync();
        var task = new TaskItem { Title = "Doomed task", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        var taskId = task.Id;

        await _sut.DeleteAsync(taskId, owner.Id, UserRole.ProjectManager);

        _activityLogMock.Verify(
            a => a.LogAsync(owner.Id, project.Id, "Deleted task", "Task", taskId, "Doomed task"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ByNonOwner_LogsNothing()
    {
        var (owner, project) = await SeedProjectAsync();
        var developer = await AddProjectMemberAsync(project.Id, UserRole.Developer, "dev@example.com");
        var task = new TaskItem { Title = "Protected", ProjectId = project.Id, CreatorId = owner.Id };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(task.Id, developer.Id, UserRole.Developer));

        _activityLogMock.VerifyNoOtherCalls();
    }
}