using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projello.Api.Data;
using Projello.Api.Services;
using System.Security.Claims;

namespace Projello.Api.Controllers;

// Who changed what, for Owners and Admins of the same company
[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 30)
    {
        if (!CompanyScope.IsCompanyManager(User.FindFirst("RoleID")?.Value)) return Forbid();

        var companyId = await _context.GetCompanyIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (companyId == null) return Ok(Array.Empty<object>());

        var entries = await _context.AuditLogs
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(a => new { a.Id, a.Action, a.EntityType, a.EntityId, a.Summary, a.UserName, a.CreatedAt })
            .ToListAsync();

        return Ok(entries);
    }
}
