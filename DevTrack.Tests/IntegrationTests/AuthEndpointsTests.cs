using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevTrack.Application.DTOs.Auth;

namespace DevTrack.Tests.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_WithValidData_Returns201AndToken()
    {
        var request = new RegisterRequest
        {
            FullName = "Integration Test User",
            Email = "integration@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Developer", body.Role);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var first = new RegisterRequest
        {
            FullName = "First",
            Email = "dupe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", first);

        var second = new RegisterRequest
        {
            FullName = "Second",
            Email = "dupe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200()
    {
        var register = new RegisterRequest
        {
            FullName = "Login Test",
            Email = "login@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", register);

        var login = new LoginRequest { Email = "login@example.com", Password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var register = new RegisterRequest
        {
            FullName = "Login Test",
            Email = "wrongpass@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", register);

        var login = new LoginRequest { Email = "wrongpass@example.com", Password = "WrongPassword!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        var register = new RegisterRequest
        {
            FullName = "Protected Test",
            Email = "protected@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", register);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}