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
    public async Task UpdateProfile_WithAName_UpdatesAndReturnsTheProfile()
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, "profile@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var response = await _client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FullName = "New Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("New Name", profile!.FullName);

        var me = await _client.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal("New Name", me!.FullName);
    }
}