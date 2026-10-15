using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevTrack.Application.DTOs.Auth;
using DevTrack.Application.DTOs.Dashboard;
using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Application.DTOs.Users;

namespace DevTrack.Tests.IntegrationTests;

public class DashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Scenario(int ProjectId, string AdminToken, string MemberToken, string TeamOnlyToken, string StrangerToken);

    private static async Task<HttpResponseMessage> Ok(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Setup request failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return response;
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var login = await Ok(await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "admin@devtrack.local", Password = "Admin123!" }));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private async Task<(string Token, int Id)> RegisterAsync(string email)
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, email);
        AuthHelper.AuthorizeAs(_client, token);
        var me = await _client.GetFromJsonAsync<UserResponse>("/api/users/me");
        return (token, me!.Id);
    }

    // One team (owned by the admin) with one project and two tasks.
    // "member" is on the project, "teamOnly" is on the team but not the project, "stranger" is on neither.
    private async Task<Scenario> BuildScenarioAsync()
    {
        var member = await RegisterAsync("member@example.com");
        var teamOnly = await RegisterAsync("team-only@example.com");
        var stranger = await RegisterAsync("stranger@example.com");

        var adminToken = await LoginAsAdminAsync();
        AuthHelper.AuthorizeAs(_client, adminToken);

        var teamResponse = await Ok(await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Team" }));
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();
        await Ok(await _client.PostAsJsonAsync($"/api/teams/{team!.Id}/members", new AddTeamMemberRequest { UserId = member.Id }));
        await Ok(await _client.PostAsJsonAsync($"/api/teams/{team.Id}/members", new AddTeamMemberRequest { UserId = teamOnly.Id }));

        var projectResponse = await Ok(await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow }));
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        await Ok(await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/members", new AddProjectMemberRequest { UserId = member.Id }));

        await Ok(await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest { Title = "First", ProjectId = project.Id }));
        await Ok(await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest { Title = "Second", ProjectId = project.Id }));

        return new Scenario(project.Id, adminToken, member.Token, teamOnly.Token, stranger.Token);
    }

    private async Task<int> TotalCountAsync(string url, bool treatForbiddenAsZero = false)
    {
        var response = await _client.GetAsync(url);
        if (treatForbiddenAsZero && response.StatusCode == HttpStatusCode.Forbidden)
            return 0;
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("totalCount").GetInt32();
    }

    private static (string Label, string Token)[] EveryKindOfUser(Scenario s) => new[]
    {
        ("admin", s.AdminToken),
        ("project member", s.MemberToken),
        ("team-only member", s.TeamOnlyToken),
        ("stranger", s.StrangerToken)
    };

    [Fact]
    public async Task Stats_TotalTasks_MatchesTheTaskListForEveryKindOfUser()
    {
        var scenario = await BuildScenarioAsync();

        AuthHelper.AuthorizeAs(_client, scenario.AdminToken);
        var adminStats = await _client.GetFromJsonAsync<DashboardStatsResponse>("/api/dashboard/stats");
        Assert.Equal(2, adminStats!.TotalTasks); // sanity check on the setup itself

        foreach (var (label, token) in EveryKindOfUser(scenario))
        {
            AuthHelper.AuthorizeAs(_client, token);
            var stats = await _client.GetFromJsonAsync<DashboardStatsResponse>("/api/dashboard/stats");
            var listed = await TotalCountAsync("/api/tasks?pageSize=1");

            Assert.True(stats!.TotalTasks == listed,
                $"{label}: the dashboard counts {stats.TotalTasks} tasks but the task list has {listed}");
        }
    }

    [Fact]
    public async Task RecentActivity_MatchesWhatTheProjectActivityEndpointAllows()
    {
        var scenario = await BuildScenarioAsync();

        AuthHelper.AuthorizeAs(_client, scenario.AdminToken);
        Assert.Equal(3, await TotalCountAsync("/api/activity?pageSize=1")); // project created + two tasks

        foreach (var (label, token) in EveryKindOfUser(scenario))
        {
            AuthHelper.AuthorizeAs(_client, token);
            var global = await TotalCountAsync("/api/activity?pageSize=1");
            var forProject = await TotalCountAsync(
                $"/api/projects/{scenario.ProjectId}/activity?pageSize=1", treatForbiddenAsZero: true);

            Assert.True(global == forProject,
                $"{label}: the global feed has {global} entries but the project's own feed allows {forProject}");
        }
    }

    [Fact]
    public async Task Stats_UsesTheDateTheCallerSends_ToDecideWhatIsOverdue()
    {
        AuthHelper.AuthorizeAs(_client, await LoginAsAdminAsync());

        var teamResponse = await Ok(await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Team" }));
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();
        var projectResponse = await Ok(await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest { Name = "Project", TeamId = team!.Id, StartDate = DateTime.UtcNow }));
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        await Ok(await _client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest { Title = "Due on the 2nd", ProjectId = project!.Id, DueDate = new DateTime(2030, 1, 2) }));

        var onTheDay = await _client.GetFromJsonAsync<DashboardStatsResponse>("/api/dashboard/stats?today=2030-01-02");
        var dayAfter = await _client.GetFromJsonAsync<DashboardStatsResponse>("/api/dashboard/stats?today=2030-01-03");

        Assert.Equal(0, onTheDay!.OverdueTasks);
        Assert.Equal(1, dayAfter!.OverdueTasks);
    }

    [Fact]
    public async Task Stats_WithAMalformedDate_Returns400()
    {
        AuthHelper.AuthorizeAs(_client, await LoginAsAdminAsync());

        var response = await _client.GetAsync("/api/dashboard/stats?today=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DashboardAndActivityEndpoints_WithoutAToken_Return401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/dashboard/stats")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/activity")).StatusCode);
    }
}