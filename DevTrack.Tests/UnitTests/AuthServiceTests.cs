using DevTrack.Application.Common.Exceptions;
using DevTrack.Application.DTOs.Auth;
using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class AuthServiceTests
{
    private readonly DevTrackDbContext _db;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _sut; // "sut" = system under test, a common convention

    public AuthServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(_db, _passwordHasherMock.Object, _tokenServiceMock.Object, logger.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserWithDeveloperRole()
    {
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("fake-token");

        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("Developer", result.Role);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("fake-token", result.Token);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        _db.Users.Add(new User { FullName = "Existing", Email = "taken@example.com", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        var request = new RegisterRequest
        {
            FullName = "New Person",
            Email = "taken@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_ThrowsInvalidOperationException()
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "mismatch@example.com",
            Password = "Password123!",
            ConfirmPassword = "Different123!"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_NeverAssignsAnyRoleOtherThanDeveloper()
    {
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("token");

        var request = new RegisterRequest
        {
            FullName = "Hopeful Admin",
            Email = "hopeful@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("Developer", result.Role);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        _db.Users.Add(new User { FullName = "Existing", Email = "user@example.com", PasswordHash = "correct-hash", IsActive = true });
        await _db.SaveChangesAsync();
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var request = new LoginRequest { Email = "user@example.com", Password = "wrong-password" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedAccount_ThrowsInvalidCredentialsException()
    {
        _db.Users.Add(new User { FullName = "Existing", Email = "inactive@example.com", PasswordHash = "correct-hash", IsActive = false });
        await _db.SaveChangesAsync();
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var request = new LoginRequest { Email = "inactive@example.com", Password = "whatever" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenAndUserInfo()
    {
        _db.Users.Add(new User { FullName = "Existing", Email = "user@example.com", PasswordHash = "correct-hash", IsActive = true, Role = UserRole.Developer });
        await _db.SaveChangesAsync();
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("real-token");

        var request = new LoginRequest { Email = "user@example.com", Password = "correct-password" };

        var result = await _sut.LoginAsync(request);

        Assert.Equal("real-token", result.Token);
        Assert.Equal("user@example.com", result.Email);
    }
}