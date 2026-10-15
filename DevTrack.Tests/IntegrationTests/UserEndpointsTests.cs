using System.Net;
using System.Net.Http.Json;
using DevTrack.Application.DTOs.Users;

namespace DevTrack.Tests.IntegrationTests;

public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateProfile_WithValidValues_UpdatesAndReturnsTheProfile()
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, "profile@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var request = new UpdateProfileRequest { FullName = "New Name", ProfileImageUrl = "https://example.com/me.png" };
        var response = await _client.PutAsJsonAsync("/api/users/me", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("New Name", profile!.FullName);
        Assert.Equal("https://example.com/me.png", profile.ProfileImageUrl);

        var me = await _client.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal("https://example.com/me.png", me!.ProfileImageUrl);
    }

    [Fact]
    public async Task UpdateProfile_WithAbsurdlyLongImageUrl_Returns400()
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, "longurl@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var request = new UpdateProfileRequest
        {
            FullName = "Long Url",
            ProfileImageUrl = "https://example.com/" + new string('a', 3000)
        };
        var response = await _client.PutAsJsonAsync("/api/users/me", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}