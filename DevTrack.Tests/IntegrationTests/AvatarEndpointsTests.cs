using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DevTrack.Application.DTOs.Users;

namespace DevTrack.Tests.IntegrationTests;

public class AvatarEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AvatarEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // The opening bytes of a JPEG followed by filler. The server only checks the opening bytes and the size.
    private static byte[] FakeJpeg(int length = 300) =>
        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(Enumerable.Repeat((byte)7, length - 4)).ToArray();

    private static ByteArrayContent Body(byte[] bytes, string contentType = "image/jpeg")
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }

    private async Task SignInAsync(string email)
    {
        var token = await AuthHelper.RegisterAndLoginAsync(_client, email);
        AuthHelper.AuthorizeAs(_client, token);
    }

    [Fact]
    public async Task UploadedPicture_CanBeFetchedBack_WithTheSameBytes()
    {
        await SignInAsync("avatar@example.com");
        var jpeg = FakeJpeg();

        var upload = await _client.PutAsync("/api/users/me/avatar", Body(jpeg));

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var profile = await upload.Content.ReadFromJsonAsync<UserResponse>();
        Assert.StartsWith($"/api/users/{profile!.Id}/avatar?v=", profile.ProfileImageUrl);

        var fetched = await _client.GetAsync(profile.ProfileImageUrl);

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("image/jpeg", fetched.Content.Headers.ContentType?.MediaType);
        Assert.Equal(jpeg, await fetched.Content.ReadAsByteArrayAsync());

        var me = await _client.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal(profile.ProfileImageUrl, me!.ProfileImageUrl);
    }

    [Fact]
    public async Task Picture_IsNotServedWithoutASignedInUser()
    {
        await SignInAsync("private@example.com");
        var upload = await _client.PutAsync("/api/users/me/avatar", Body(FakeJpeg()));
        var profile = await upload.Content.ReadFromJsonAsync<UserResponse>();

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync(profile!.ProfileImageUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithSomethingThatIsNotAnImage_Returns400()
    {
        await SignInAsync("notimage@example.com");

        var response = await _client.PutAsync("/api/users/me/avatar",
            Body(Encoding.UTF8.GetBytes("definitely not a picture"), "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OverTheSizeLimit_Returns400()
    {
        await SignInAsync("toobig@example.com");

        var response = await _client.PutAsync("/api/users/me/avatar", Body(FakeJpeg(300 * 1024)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemovedPicture_IsGone_AndTheProfileAddressIsCleared()
    {
        await SignInAsync("remove@example.com");
        var upload = await _client.PutAsync("/api/users/me/avatar", Body(FakeJpeg()));
        var uploaded = await upload.Content.ReadFromJsonAsync<UserResponse>();

        var removal = await _client.DeleteAsync("/api/users/me/avatar");

        Assert.Equal(HttpStatusCode.OK, removal.StatusCode);
        var profile = await removal.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Null(profile!.ProfileImageUrl);

        var fetched = await _client.GetAsync(uploaded!.ProfileImageUrl);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }
}