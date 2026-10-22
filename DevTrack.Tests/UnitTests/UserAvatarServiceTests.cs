using DevTrack.Application.Common;
using DevTrack.Application.DTOs.Users;
using DevTrack.Domain.Entities;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using DevTrack.Application.Interfaces;
using Moq;

namespace DevTrack.Tests.UnitTests;

public class UserAvatarServiceTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Webp = System.Text.Encoding.ASCII.GetBytes("RIFF....WEBP");

    private readonly DevTrackDbContext _db;
    private readonly UserService _sut;

    public UserAvatarServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new UserService(_db, new Mock<IPasswordHasher>().Object);
    }

    private async Task<User> AddUserAsync()
    {
        var user = new User { FullName = "Pic Person", Email = "pic@example.com", PasswordHash = "x" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task SetAvatarAsync_WithAJpeg_StoresItAndPointsTheProfileAtIt()
    {
        var user = await AddUserAsync();

        var result = await _sut.SetAvatarAsync(user.Id, Jpeg);

        Assert.StartsWith($"/api/users/{user.Id}/avatar?v=", result.ProfileImageUrl);
        var stored = await _sut.GetAvatarAsync(user.Id);
        Assert.NotNull(stored);
        Assert.Equal(Jpeg, stored!.Data);
        Assert.Equal("image/jpeg", stored.ContentType);
    }

    [Fact]
    public async Task SetAvatarAsync_ReplacesAnEarlierPicture_AndDetectsTheTypeFromTheBytes()
    {
        var user = await AddUserAsync();

        await _sut.SetAvatarAsync(user.Id, Png);
        Assert.Equal("image/png", (await _sut.GetAvatarAsync(user.Id))!.ContentType);

        await _sut.SetAvatarAsync(user.Id, Webp);
        Assert.Equal("image/webp", (await _sut.GetAvatarAsync(user.Id))!.ContentType);
        Assert.Equal(1, await _db.UserAvatars.CountAsync());
    }

    [Fact]
    public async Task SetAvatarAsync_WithSomethingThatIsNotAnImage_ThrowsAndStoresNothing()
    {
        var user = await AddUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SetAvatarAsync(user.Id, System.Text.Encoding.UTF8.GetBytes("definitely not a picture")));

        Assert.Empty(_db.UserAvatars);
    }

    [Fact]
    public async Task SetAvatarAsync_WithAnEmptyBody_Throws()
    {
        var user = await AddUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetAvatarAsync(user.Id, Array.Empty<byte>()));
    }

    [Fact]
    public async Task SetAvatarAsync_OverTheSizeLimit_Throws()
    {
        var user = await AddUserAsync();
        var tooBig = new byte[AvatarRules.MaxBytes + 1];
        tooBig[0] = 0xFF;
        tooBig[1] = 0xD8;
        tooBig[2] = 0xFF;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetAvatarAsync(user.Id, tooBig));
    }

    [Fact]
    public async Task SetAvatarAsync_ForAnUnknownUser_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.SetAvatarAsync(999, Jpeg));
    }

    [Fact]
    public async Task RemoveAvatarAsync_DeletesThePictureAndClearsTheAddress()
    {
        var user = await AddUserAsync();
        await _sut.SetAvatarAsync(user.Id, Jpeg);

        var result = await _sut.RemoveAvatarAsync(user.Id);

        Assert.Null(result.ProfileImageUrl);
        Assert.Null(await _sut.GetAvatarAsync(user.Id));
    }

    [Fact]
    public async Task GetAvatarAsync_ForAUserWithoutAPicture_ReturnsNull()
    {
        var user = await AddUserAsync();

        Assert.Null(await _sut.GetAvatarAsync(user.Id));
    }

    [Fact]
    public async Task UpdateProfileAsync_ChangingTheName_KeepsTheProfilePicture()
    {
        var user = await AddUserAsync();
        var withPicture = await _sut.SetAvatarAsync(user.Id, Jpeg);

        var renamed = await _sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest { FullName = "Renamed" });

        Assert.Equal("Renamed", renamed.FullName);
        Assert.Equal(withPicture.ProfileImageUrl, renamed.ProfileImageUrl);
    }
}