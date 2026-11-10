using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Projello.Api.DTOs;
using Projello.Api.Tests.Integration;
using Xunit;

namespace Projello.Api.Tests.ControllerTests
{
    // Runs through the real API with no Cloudinary settings, like a deploy without photo keys
    public class SiteUpdatesIntegrationTests : IClassFixture<ProjelloApiFactory>
    {
        private readonly HttpClient _client;

        public SiteUpdatesIntegrationTests(ProjelloApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> CreateOwnedProjectAsync()
        {
            var email = $"owner_{Guid.NewGuid()}@test.com";
            await _client.PostAsJsonAsync("/api/auth/register-company", new UserRegisterDto { CompanyName = "Site Co", FullName = "Owner", Email = email, Password = "Password123!" });
            var login = await (await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = email, Password = "Password123!" }))
                .Content.ReadFromJsonAsync<LoginResponseData>();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

            var client = await (await _client.PostAsJsonAsync("/api/clients", new CreateClientDto { Name = $"Client {Guid.NewGuid()}" }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var project = await _client.PostAsJsonAsync("/api/projects", new ProjectCreateDto { Name = "Site", ClientID = client.GetProperty("clientID").GetInt32() });
            project.EnsureSuccessStatusCode();
            return (await project.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectID").GetInt32();
        }

        [Fact]
        public async Task GetUpdates_WithoutCloudinaryKeys_ReturnsOk()
        {
            var projectId = await CreateOwnedProjectAsync();

            var response = await _client.GetAsync($"/api/projects/{projectId}/updates");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUpdate_SameIdempotencyKeyTwice_CreatesOnePost()
        {
            var projectId = await CreateOwnedProjectAsync();
            var key = Guid.NewGuid().ToString();

            async Task<HttpResponseMessage> Send()
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/projects/{projectId}/updates")
                {
                    Content = new MultipartFormDataContent { { new StringContent("Slab poured"), "Caption" } }
                };
                request.Headers.Add("Idempotency-Key", key);
                return await _client.SendAsync(request);
            }

            var first = await Send();
            var retry = await Send(); // e.g. the connection dropped and the app retried

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.True(retry.Headers.Contains("Idempotent-Replay"));

            var posts = await (await _client.GetAsync($"/api/projects/{projectId}/updates")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, posts.GetArrayLength());
        }

        [Fact]
        public async Task CreateTextUpdate_WithoutCloudinaryKeys_ReturnsOk()
        {
            var projectId = await CreateOwnedProjectAsync();

            var form = new MultipartFormDataContent { { new StringContent("Slab poured"), "Caption" } };
            var response = await _client.PostAsync($"/api/projects/{projectId}/updates", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
