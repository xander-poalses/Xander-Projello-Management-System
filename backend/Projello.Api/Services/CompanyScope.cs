using Microsoft.EntityFrameworkCore;
using Projello.Api.Data;

namespace Projello.Api.Services;

// One rule for every controller: nobody sees or changes data outside their own company.
// Admin (1) and Owner (4) manage everything in their company; Foremen and Workers
// only reach projects they are members of.
public static class CompanyScope
{
    public static bool IsCompanyManager(string? role) => role == "1" || role == "4";

    public static Task<int?> GetCompanyIdAsync(this AppDbContext db, string? userId) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.CompanyId).FirstOrDefaultAsync();

    public static Task<int?> GetProjectCompanyIdAsync(this AppDbContext db, int projectId) =>
        db.Projects.Where(p => p.ProjectID == projectId).Select(p => (int?)p.Client.CompanyID).FirstOrDefaultAsync();

    public static async Task<bool> IsProjectInCompanyAsync(this AppDbContext db, int projectId, string? userId) =>
        await db.IsProjectInCompanyAsync(projectId, await db.GetCompanyIdAsync(userId));

    // For controllers that already loaded the user's company (for example through UserManager)
    public static async Task<bool> IsProjectInCompanyAsync(this AppDbContext db, int projectId, int? companyId) =>
        companyId != null && await db.GetProjectCompanyIdAsync(projectId) == companyId;

    public static async Task<bool> CanAccessProjectAsync(this AppDbContext db, int projectId, string? userId, string? role) =>
        await db.CanAccessProjectAsync(projectId, await db.GetCompanyIdAsync(userId), userId, role);

    public static async Task<bool> CanAccessProjectAsync(this AppDbContext db, int projectId, int? companyId, string? userId, string? role)
    {
        if (!await db.IsProjectInCompanyAsync(projectId, companyId)) return false;
        if (IsCompanyManager(role)) return true;
        return await db.ProjectMembers.AnyAsync(m => m.ProjectID == projectId && m.UserID == userId);
    }
}
