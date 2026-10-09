using System.Net;
using System.Net.Http.Json;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Application.DTOs.Users;

namespace DevTrack.Tests.IntegrationTests;

public class TeamEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TeamEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(string token, int userId)> CreateProjectManagerAsync(string email)
    {
        var adminLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@devtrack.local", password = "Admin123!" });
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<DevTrack.Application.DTOs.Auth.AuthResponse>();

        var token = await AuthHelper.RegisterAndLoginAsync(_client, email);
        AuthHelper.AuthorizeAs(_client, token);
        var me = await (await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/users/me"))).Content.ReadFromJsonAsync<UserResponse>();

        AuthHelper.AuthorizeAs(_client, adminAuth!.Token);
        var promoteResponse = await _client.PatchAsJsonAsync($"/api/users/{me!.Id}/role", new UpdateUserRoleRequest { Role = "ProjectManager" });
        if (!promoteResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to promote test user to ProjectManager: {promoteResponse.StatusCode}");

        var freshLogin = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var freshAuth = await freshLogin.Content.ReadFromJsonAsync<DevTrack.Application.DTOs.Auth.AuthResponse>();

        AuthHelper.AuthorizeAs(_client, freshAuth!.Token);
        return (freshAuth.Token, me.Id);
    }

    [Fact]
    public async Task Create_AsDeveloper_Returns403()
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, "dev@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var response = await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Blocked Team" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsProjectManager_Returns201()
    {
        var (token, _) = await CreateProjectManagerAsync("pm@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var response = await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "PM's Team" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var team = await response.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.Equal(1, team!.MemberCount);
    }

    [Fact]
    public async Task GetAll_AsOutsider_ReturnsEmptyArray()
    {
        var (ownerToken, _) = await CreateProjectManagerAsync("owner@example.com");
        AuthHelper.AuthorizeAs(_client, ownerToken);
        var createResponse = await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Private Team" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var outsiderToken = await AuthHelper.RegisterAndLoginAsync(_client, "outsider@example.com");
        AuthHelper.AuthorizeAs(_client, outsiderToken);

        var response = await _client.GetAsync("/api/teams");
        var teams = await response.Content.ReadFromJsonAsync<List<TeamResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(teams!);
    }

    [Fact]
    public async Task AddMember_Twice_SecondCallReturns409()
    {
        var (ownerToken, _) = await CreateProjectManagerAsync("owner2@example.com");
        AuthHelper.AuthorizeAs(_client, ownerToken);
        var createResponse = await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Team" });
        var team = await createResponse.Content.ReadFromJsonAsync<TeamResponse>();

        var memberToken = await AuthHelper.RegisterAndLoginAsync(_client, "member@example.com");
        AuthHelper.AuthorizeAs(_client, memberToken);
        var meResponse = await _client.GetAsync("/api/users/me");
        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>();

        AuthHelper.AuthorizeAs(_client, ownerToken);
        await _client.PostAsJsonAsync($"/api/teams/{team!.Id}/members", new AddTeamMemberRequest { UserId = me!.Id });
        var secondAttempt = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/members", new AddTeamMemberRequest { UserId = me.Id });

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }
    [Fact]
    public async Task LookupUser_AsDeveloper_Returns403()
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, "plain-dev@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var response = await _client.GetAsync("/api/users/lookup?email=admin@devtrack.local");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LookupUser_AsProjectManager_ReturnsBasicInfo()
    {
        var (token, _) = await CreateProjectManagerAsync("lookup-pm@example.com");
        AuthHelper.AuthorizeAs(_client, token);

        var response = await _client.GetAsync("/api/users/lookup?email=admin@devtrack.local");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lookup = await response.Content.ReadFromJsonAsync<UserLookupResponse>();
        Assert.Equal("System Admin", lookup!.FullName);
        Assert.Equal("admin@devtrack.local", lookup.Email);
    }

    [Fact]
    public async Task GetById_UnknownTeam_Returns404WithMessage()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@devtrack.local", password = "Admin123!" });
        var auth = await login.Content.ReadFromJsonAsync<DevTrack.Application.DTOs.Auth.AuthResponse>();
        AuthHelper.AuthorizeAs(_client, auth!.Token);

        var response = await _client.GetAsync("/api/teams/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Team not found", body);
    }
}