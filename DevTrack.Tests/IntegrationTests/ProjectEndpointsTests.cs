using System.Net;
using System.Net.Http.Json;
using DevTrack.Application.DTOs.Auth;
using DevTrack.Application.DTOs.Comments;
using DevTrack.Application.DTOs.Projects;
using DevTrack.Application.DTOs.Tasks;
using DevTrack.Application.DTOs.Teams;
using DevTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Tests.IntegrationTests;

public class ProjectEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_ProjectWithTasksAndComments_Returns204_AndKeepsTheAuditTrail()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "admin@devtrack.local", Password = "Admin123!" });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        AuthHelper.AuthorizeAs(_client, auth!.Token);

        var teamResponse = await _client.PostAsJsonAsync("/api/teams", new CreateTeamRequest { Name = "Team" });
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();

        var projectResponse = await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest { Name = "Doomed project", TeamId = team!.Id, StartDate = DateTime.UtcNow });
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var taskResponse = await _client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest { Title = "Task", ProjectId = project!.Id });
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var commentResponse = await _client.PostAsJsonAsync($"/api/tasks/{task!.Id}/comments",
            new CreateCommentRequest { Content = "A comment" });
        Assert.Equal(HttpStatusCode.Created, commentResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/projects/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        Assert.False(await db.ActivityLogs.AnyAsync(a => a.ProjectId == project.Id));
        Assert.True(await db.ActivityLogs.AnyAsync(a => a.ProjectId == null && a.Action == "Deleted project"));
    }
}