using System.Linq;
using Projello.Api.Data;
using Projello.Api.Models;

namespace Projello.Api.Tests
{
    // Controllers only allow access inside the caller's company. Tests that seed bare
    // projects and users call Adopt to put all of them in one test company.
    public static class TestCompany
    {
        public const int Id = 10;
        private const int ClientId = 9001;

        public static void Adopt(AppDbContext context, params string[] userIds)
        {
            if (!context.Companies.Any(c => c.CompanyID == Id))
                context.Companies.Add(new Company { CompanyID = Id, Name = "Test Co" });
            if (!context.Clients.Any(c => c.ClientID == ClientId))
                context.Clients.Add(new Client { ClientID = ClientId, CompanyID = Id, Name = "Test Client" });
            context.SaveChanges();

            // Milestones in some tests point at projects that were never seeded
            var projectIds = context.Projects.Select(p => p.ProjectID).ToList();
            foreach (var missing in context.Milestones.Select(m => m.ProjectID).Distinct().ToList().Where(id => !projectIds.Contains(id)))
                context.Projects.Add(new Project { ProjectID = missing, Name = $"Project {missing}", Status = "Active", CreatedByUserID = "test", ClientID = ClientId });
            context.SaveChanges();

            var clientIds = context.Clients.Select(c => c.ClientID).ToList();
            foreach (var project in context.Projects.Where(p => !clientIds.Contains(p.ClientID)).ToList())
                project.ClientID = ClientId;

            foreach (var id in userIds)
            {
                var user = context.Users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    context.Users.Add(new User { Id = id, FullName = id, Email = $"{id}@test.com", CompanyId = Id });
                else if (user.CompanyId == null)
                    user.CompanyId = Id;
            }

            context.SaveChanges();
        }
    }
}
