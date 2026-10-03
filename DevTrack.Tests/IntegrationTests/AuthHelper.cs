using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevTrack.Application.DTOs.Auth;

namespace DevTrack.Tests.IntegrationTests;

public static class AuthHelper
{
    public static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string fullName = "Test User")
    {
        var register = new RegisterRequest
        {
            FullName = fullName,
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", register);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    public static void AuthorizeAs(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}