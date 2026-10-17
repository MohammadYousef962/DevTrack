using System.Net.Http.Json;
using System.Text.Json;
using DevTrack.Application.DTOs.Auth;
using DevTrack.Application.DTOs.Comments;
using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Application.DTOs.Users;

namespace DevTrack.Tests.IntegrationTests;

public class ActivityLoggingEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ActivityLoggingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // A failed setup call stops the test with the server's answer, so it can't pass or fail for the wrong reason.
    private static async Task<HttpResponseMessage> Ok(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Setup request failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return response;
    }

    [Fact]
    public async Task ProjectFeed_RecordsEachKindOfChange_WithReadableDetails()
    {
        // A second person to add to the project and remove again.
        var memberToken = await AuthHelper.RegisterAndLoginAsync(_client, "member@example.com");
        AuthHelper.AuthorizeAs(_client, memberToken);
        var member = (await _client.GetFromJsonAsync<UserResponse>("/api/users/me"))!;

        var login = await Ok(await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "admin@devtrack.local", Password = "Admin123!" }));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        AuthHelper.AuthorizeAs(_client, auth!.Token);

        var teamResponse = await Ok(await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Team" }));
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();
        await Ok(await _client.PostAsJsonAsync($"/api/teams/{team!.Id}/members", new AddTeamMemberRequest { UserId = member.Id }));

        var projectResponse = await Ok(await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest { Name = "Project", TeamId = team.Id, StartDate = DateTime.UtcNow }));
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        var projectId = project!.Id;

        await Ok(await _client.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest { UserId = member.Id }));

        var taskResponse = await Ok(await _client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest { Title = "Write the docs", ProjectId = projectId }));
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskResponse>();
        var taskId = task!.Id;

        await Ok(await _client.PutAsJsonAsync($"/api/tasks/{taskId}", new UpdateTaskRequest { Title = "Write the README" }));
        await Ok(await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/priority", new { priority = "High" }));
        await Ok(await _client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest { Content = "On it" }));

        await Ok(await _client.PutAsJsonAsync($"/api/projects/{projectId}",
            new UpdateProjectRequest { Name = "Renamed project", StartDate = project.StartDate }));
        await Ok(await _client.PatchAsJsonAsync($"/api/projects/{projectId}/status", new { status = "Active" }));
        await Ok(await _client.DeleteAsync($"/api/projects/{projectId}/members/{member.Id}"));
        await Ok(await _client.DeleteAsync($"/api/tasks/{taskId}"));

        using var json = JsonDocument.Parse(await _client.GetStringAsync($"/api/projects/{projectId}/activity?pageSize=50"));
        var entries = json.RootElement.GetProperty("items").EnumerateArray()
            .Select(e => (
                Action: e.GetProperty("action").GetString()!,
                Details: e.TryGetProperty("details", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null))
            .ToList();

        string? DetailsOf(string action) => entries.Single(e => e.Action == action).Details;

        Assert.Equal(
            new[]
            {
                "Added comment", "Added project member", "Changed project status", "Changed task priority",
                "Created project", "Created task", "Deleted task", "Removed project member",
                "Updated project", "Updated task"
            },
            entries.Select(e => e.Action).OrderBy(a => a, StringComparer.Ordinal).ToArray());

        Assert.Equal("title", DetailsOf("Updated task"));
        Assert.Equal("Medium \u2192 High", DetailsOf("Changed task priority"));
        Assert.Equal("name", DetailsOf("Updated project"));
        Assert.Equal("Planning \u2192 Active", DetailsOf("Changed project status"));
        Assert.Equal(member.FullName, DetailsOf("Added project member"));
        Assert.Equal(member.FullName, DetailsOf("Removed project member"));
        Assert.Equal("Write the README", DetailsOf("Deleted task"));
        Assert.Null(DetailsOf("Added comment"));
    }
}