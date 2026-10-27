using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class DbSeederTests
{
    private readonly DevTrackDbContext _db = TestDbContextFactory.Create();
    private readonly Mock<IPasswordHasher> _hasherMock = new();

    public DbSeederTests()
    {
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
    }

    private static IConfiguration Settings(string? email, string? password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSeed:Email"] = email,
                ["AdminSeed:Password"] = password
            })
            .Build();

    [Fact]
    public async Task SeedAsync_InDevelopment_CreatesTheWellKnownAdmin()
    {
        await DbSeeder.SeedAsync(_db, _hasherMock.Object);

        var admin = await _db.Users.SingleAsync();
        Assert.Equal("admin@devtrack.local", admin.Email);
        Assert.Equal(UserRole.Admin, admin.Role);
        _hasherMock.Verify(h => h.Hash("Admin123!"), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_InProductionWithoutSettings_CreatesNoAdmin()
    {
        await DbSeeder.SeedAsync(_db, _hasherMock.Object, Settings(null, null), allowDefaults: false);

        Assert.Empty(_db.Users);
    }

    [Fact]
    public async Task SeedAsync_InProductionWithSettings_CreatesTheConfiguredAdmin()
    {
        await DbSeeder.SeedAsync(_db, _hasherMock.Object,
            Settings("owner@example.com", "a-long-enough-password"), allowDefaults: false);

        var admin = await _db.Users.SingleAsync();
        Assert.Equal("owner@example.com", admin.Email);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task SeedAsync_InProductionWithAShortPassword_ThrowsAndCreatesNothing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbSeeder.SeedAsync(_db, _hasherMock.Object, Settings("owner@example.com", "short"), allowDefaults: false));

        Assert.Empty(_db.Users);
    }

    [Fact]
    public async Task SeedAsync_WhenAnAdminAlreadyExists_DoesNothing()
    {
        _db.Users.Add(new User { FullName = "Existing", Email = "existing@example.com", PasswordHash = "x", Role = UserRole.Admin });
        await _db.SaveChangesAsync();

        await DbSeeder.SeedAsync(_db, _hasherMock.Object,
            Settings("owner@example.com", "a-long-enough-password"), allowDefaults: false);

        Assert.Equal(1, await _db.Users.CountAsync());
    }
}