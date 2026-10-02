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
}