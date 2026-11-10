using System;
using System.Linq;
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
    // Through the real API: payments live on projects, deletes are soft, changes are audited
    public class PaymentsAndSoftDeleteTests : IClassFixture<ProjelloApiFactory>
    {
        private readonly HttpClient _client;

        public PaymentsAndSoftDeleteTests(ProjelloApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<(int clientId, int projectId)> SignInWithProjectAsync()
        {
            var email = $"owner_{Guid.NewGuid()}@test.com";
            await _client.PostAsJsonAsync("/api/auth/register-company", new UserRegisterDto { CompanyName = "Pay Co", FullName = "Owner", Email = email, Password = "Password123!" });
            var login = await (await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = email, Password = "Password123!" }))
                .Content.ReadFromJsonAsync<LoginResponseData>();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

            var client = await (await _client.PostAsJsonAsync("/api/clients", new CreateClientDto { Name = $"Client {Guid.NewGuid()}" }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var clientId = client.GetProperty("clientID").GetInt32();
            var project = await (await _client.PostAsJsonAsync("/api/projects", new ProjectCreateDto { Name = "Block A", ClientID = clientId }))
                .Content.ReadFromJsonAsync<JsonElement>();
            return (clientId, project.GetProperty("projectID").GetInt32());
        }

        [Fact]
        public async Task ProjectPayments_AddUpOnTheClient_AndSurviveARename()
        {
            var (clientId, projectId) = await SignInWithProjectAsync();

            var pay = await _client.PutAsJsonAsync($"/api/projects/{projectId}/payments", new ProjectPaymentsDto { TotalPaid = 1500.255m, Outstanding = 250m });
            pay.EnsureSuccessStatusCode();

            // Renaming used to send no payment fields and wipe them
            var rename = await _client.PutAsJsonAsync($"/api/projects/{projectId}", new ProjectUpdateDto { Name = "Block A (renamed)", ClientID = clientId });
            rename.EnsureSuccessStatusCode();

            var client = await (await _client.GetAsync($"/api/clients/{clientId}")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1500.26m, client.GetProperty("totalPaid").GetDecimal()); // rounded to cents
            Assert.Equal(250m, client.GetProperty("outstanding").GetDecimal());

            var summary = await (await _client.GetAsync("/api/clients/summary")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1500.26m, summary.GetProperty("totalRevenue").GetDecimal());
        }

        [Fact]
        public async Task DeletedProject_IsHidden_ListedAsDeleted_AndCanBeRestored()
        {
            var (_, projectId) = await SignInWithProjectAsync();

            (await _client.DeleteAsync($"/api/projects/{projectId}")).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/projects/{projectId}")).StatusCode);
            var deleted = await (await _client.GetAsync("/api/projects/deleted")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(deleted.EnumerateArray(), p => p.GetProperty("projectID").GetInt32() == projectId);

            (await _client.PostAsync($"/api/projects/{projectId}/restore", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/projects/{projectId}")).StatusCode);
        }

        [Fact]
        public async Task Changes_AreRecordedInTheAuditTrail()
        {
            var (_, projectId) = await SignInWithProjectAsync();
            (await _client.DeleteAsync($"/api/projects/{projectId}")).EnsureSuccessStatusCode();

            var audit = await (await _client.GetAsync("/api/audit")).Content.ReadFromJsonAsync<JsonElement>();
            var entries = audit.EnumerateArray().ToList();

            Assert.Contains(entries, e => e.GetProperty("entityType").GetString() == "Project" && e.GetProperty("action").GetString() == "Created");
            Assert.Contains(entries, e => e.GetProperty("entityType").GetString() == "Project" && e.GetProperty("action").GetString() == "Deleted"
                                          && e.GetProperty("userName").GetString() == "Owner");
        }
    }
}
