using DevTrack.Application.Common.Exceptions;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Tests.UnitTests;

public class TeamServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly TeamService _sut;

    public TeamServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new TeamService(_db);
    }

    private async Task<User> SeedUserAsync(string email, UserRole role = UserRole.ProjectManager)
    {
        var user = new User { FullName = email, Email = email, PasswordHash = "x", Role = role };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsInvalidOperationException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var request = new CreateTeamRequest { Name = "" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(owner.Id, request));
    }

    [Fact]
    public async Task CreateAsync_AddsOwnerAsMemberAutomatically()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var request = new CreateTeamRequest { Name = "New Team" };

        var result = await _sut.CreateAsync(owner.Id, request);

        Assert.Equal(1, result.MemberCount);
        var isMember = await _db.TeamMembers.AnyAsync(m => m.TeamId == result.Id && m.UserId == owner.Id);
        Assert.True(isMember);
    }

    [Fact]
    public async Task GetAllForUserAsync_AsNonAdmin_OnlyReturnsOwnedOrMemberTeams()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var outsider = await SeedUserAsync("outsider@example.com", UserRole.Developer);

        var ownedTeam = new Team { Name = "Owned", OwnerId = owner.Id };
        var otherTeam = new Team { Name = "Not Mine", OwnerId = outsider.Id };
        _db.Teams.AddRange(ownedTeam, otherTeam);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllForUserAsync(owner.Id, UserRole.ProjectManager);

        Assert.Single(result);
        Assert.Equal("Owned", result.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_ByNonMemberNonOwner_ThrowsUnauthorizedAccessException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var outsider = await SeedUserAsync("outsider@example.com", UserRole.Developer);

        var team = new Team { Name = "Private Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetByIdAsync(team.Id, outsider.Id, UserRole.Developer));
    }

    [Fact]
    public async Task UpdateAsync_ByNonOwnerNonAdmin_ThrowsUnauthorizedAccessException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var outsider = await SeedUserAsync("outsider@example.com", UserRole.Developer);

        var team = new Team { Name = "Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var request = new UpdateTeamRequest { Name = "Hijacked" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateAsync(team.Id, outsider.Id, UserRole.Developer, request));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProjects_ThrowsInvalidOperationException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var team = new Team { Name = "Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        _db.Projects.Add(new Project { Name = "Blocking Project", TeamId = team.Id, StartDate = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteAsync(team.Id, owner.Id, UserRole.ProjectManager));
    }

    [Fact]
    public async Task AddMemberAsync_WhenAlreadyMember_ThrowsConflictException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var member = await SeedUserAsync("member@example.com", UserRole.Developer);

        var team = new Team { Name = "Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        _db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = member.Id });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.AddMemberAsync(team.Id, owner.Id, UserRole.ProjectManager, member.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovingOwner_ThrowsInvalidOperationException()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var team = new Team { Name = "Team", OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        _db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = owner.Id });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RemoveMemberAsync(team.Id, owner.Id, UserRole.ProjectManager, owner.Id));
    }
}