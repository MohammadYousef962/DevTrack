using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class UserServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new UserService(_db, new Mock<IPasswordHasher>().Object);
    }

    [Fact]
    public async Task LookupByEmailAsync_WithActiveUser_ReturnsBasicInfo()
    {
        _db.Users.Add(new User { FullName = "Jane Doe", Email = "jane@example.com", PasswordHash = "x", Role = UserRole.Developer });
        await _db.SaveChangesAsync();

        var result = await _sut.LookupByEmailAsync("  jane@example.com  ");

        Assert.Equal("Jane Doe", result.FullName);
        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task LookupByEmailAsync_WithDeactivatedUser_ThrowsKeyNotFoundException()
    {
        _db.Users.Add(new User { FullName = "Gone", Email = "gone@example.com", PasswordHash = "x", IsActive = false });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.LookupByEmailAsync("gone@example.com"));
    }

    [Fact]
    public async Task LookupByEmailAsync_WithUnknownEmail_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.LookupByEmailAsync("nobody@example.com"));
    }

    [Fact]
    public async Task LookupByEmailAsync_WithBlankEmail_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LookupByEmailAsync("   "));
    }
    [Fact]
    public async Task UpdateRoleAsync_DemotingADeactivatedAdmin_Succeeds_WhenAnotherAdminIsActive()
    {
        var activeAdmin = new User { FullName = "Active Admin", Email = "active-admin@example.com", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        var deactivatedAdmin = new User { FullName = "Deactivated Admin", Email = "deactivated-admin@example.com", PasswordHash = "x", Role = UserRole.Admin, IsActive = false };
        _db.Users.AddRange(activeAdmin, deactivatedAdmin);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateRoleAsync(deactivatedAdmin.Id, "Developer");

        Assert.Equal("Developer", result.Role);
    }

    [Fact]
    public async Task UpdateRoleAsync_DemotingTheLastActiveAdmin_ThrowsInvalidOperationException()
    {
        var onlyAdmin = new User { FullName = "Only Admin", Email = "only-admin@example.com", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        _db.Users.Add(onlyAdmin);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateRoleAsync(onlyAdmin.Id, "Developer"));
    }

    [Fact]
    public async Task UpdateStatusAsync_DeactivatingTheLastActiveAdmin_ThrowsInvalidOperationException()
    {
        var onlyAdmin = new User { FullName = "Only Admin", Email = "only-admin@example.com", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        _db.Users.Add(onlyAdmin);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateStatusAsync(onlyAdmin.Id, false));
    }

    [Fact]
    public async Task UpdateRoleAsync_WithANumericRole_ThrowsInvalidOperationException()
    {
        var user = new User { FullName = "Dev", Email = "dev@example.com", PasswordHash = "x", Role = UserRole.Developer };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateRoleAsync(user.Id, "99"));
    }
}