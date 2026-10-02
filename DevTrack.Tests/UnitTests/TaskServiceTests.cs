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
}