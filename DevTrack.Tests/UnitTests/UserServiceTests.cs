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
}