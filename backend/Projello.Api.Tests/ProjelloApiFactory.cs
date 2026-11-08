using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Projello.Api.Data;

namespace Projello.Api.Tests
{
    // Boots the real API, but on an in-memory database so tests never write to Supabase
    public class ProjelloApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ProjelloTests_{System.Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // UseSetting applies before Program.cs reads configuration; the tests fire many logins
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "");
            builder.UseSetting("Jwt:Key", "test-only-signing-key-that-is-at-least-32-bytes-long");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
