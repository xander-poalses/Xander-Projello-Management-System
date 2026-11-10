using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projello.Api.Data;
using Projello.Api.DTOs;
using Projello.Api.Models;
using System.Security.Claims;
using Projello.Api.Services.Realtime;

namespace Projello.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly PresenceTracker? _presence;

        public UsersController(UserManager<User> userManager, AppDbContext context, PresenceTracker? presence = null)
        {
            _userManager = userManager;
            _context = context;
            _presence = presence;
        }

        // --- READ: LIST ALL USERS (Searchable) ---
        // Access: Admin (1) and Foreman (2)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDisplayDto>>> GetUsers([FromQuery] string? search)
        {
            var role = GetUserRole();
            if (role != "1" && role != "2" && role != "4") return Forbid();

            var currentUserId = GetCurrentUserId();
            var currentUser = await _userManager.FindByIdAsync(currentUserId!);

            // Everyone, Admin included, only sees users from their own company
            if (currentUser?.CompanyId == null)
                return Ok(new List<UserDisplayDto>());

            var query = _userManager.Users.Where(u => u.CompanyId == currentUser.CompanyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
            }

            var users = await query
                .Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    RoleID = u.RoleID,
                    IsTwoFactorEnabled = u.IsTwoFactorEnabled,
                })
                .ToListAsync();

            // Online status comes from open app connections, not the database
            foreach (var user in users) user.IsOnline = _presence?.IsOnline(user.Id) ?? false;

            return Ok(users);
        }

        // --- READ: FULL DASHBOARD PROFILE ---
        // Access: Self or Admin
        [HttpGet("{id}/full")]
        public async Task<ActionResult<UserProfileDto>> GetFullProfile(string id)
        {
            if (!IsAdmin() && GetCurrentUserId() != id) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (!await IsSameCompanyAsync(user)) return Forbid();

            var projects = await _context.ProjectMembers
                .Where(pm => pm.UserID == id)
                .Select(pm => new UserProjectDto
                {
                    ProjectID = pm.ProjectID,
                    Name = pm.Project.Name,
                    RoleInProject = pm.AssignedAs
                }).ToListAsync();

            var tasks = await _context.Tasks
                .Where(t => t.AssignedToUserID == id)
                .Select(t => new UserTaskDto
                {
                    TaskID = t.TaskID,
                    Title = t.Title,
                    Status = t.Status.ToString(),
                    DueDate = t.DueDate
                }).ToListAsync();

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                RoleID = user.RoleID,
                Projects = projects,
                AssignedTasks = tasks,
                AvatarSeed = user.AvatarSeed,
                AvatarBackground = user.AvatarBackground
            });
        }

        // --- READ: WORKLOAD STATISTICS ---
        // Access: Self, Foreman, or Admin
        [HttpGet("{id}/workload")]
        public async Task<ActionResult<UserWorkloadDto>> GetUserWorkload(string id)
        {
            var role = GetUserRole();
            if (role != "1" && role != "2" && role != "4" && GetCurrentUserId() != id) return Forbid();

            var target = await _userManager.FindByIdAsync(id);
            if (target == null) return NotFound();
            if (!await IsSameCompanyAsync(target)) return Forbid();

            var stats = new UserWorkloadDto
            {
                OpenTasks = await _context.Tasks.CountAsync(t => t.AssignedToUserID == id && t.Status != Status.Completed),
                CompletedTasks = await _context.Tasks.CountAsync(t => t.AssignedToUserID == id && t.Status == Status.Completed),
                BlockedTasks = await _context.Tasks.CountAsync(t => t.AssignedToUserID == id && t.Status == Status.Blocked)
            };

            return Ok(stats);
        }

        // --- UPDATE: PROFILE SETTINGS ---
        // Access: Self or Admin
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserUpdateDto model)
        {
            if (!IsAdmin() && GetCurrentUserId() != id) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (!await CanManageAsync(user)) return Forbid();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.AvatarSeed = model.AvatarSeed;
            user.AvatarBackground = model.AvatarBackground;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // --- UPDATE: CHANGE PASSWORD ---
        // Access: Self Only
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] UpdatePasswordDto model)
        {
            if (GetCurrentUserId() != id) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            return result.Succeeded ? Ok(new { Message = "Password updated." }) : BadRequest(result.Errors);
        }

        // --- UPDATE: CHANGE ROLE (Admin Only) ---
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UserRoleUpdateDto model)
        {
            if (!IsAdmin()) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Nobody changes their own role, so no one can promote themselves
            if (GetCurrentUserId() == id)
                return BadRequest("You can't change your own role.");

            if (!await CanManageAsync(user)) return Forbid();

            // Only Admin, Foreman and Worker can be assigned, and never above your own rank
            var assignable = new[] { 1, 2, 3 };
            if (!assignable.Contains(model.RoleID) || RoleRank(model.RoleID) >= RoleRank(CurrentRoleId()))
                return Forbid();

            user.RoleID = model.RoleID;
            await _userManager.UpdateAsync(user);
            return Ok(new { Message = "User role changed." });
        }

        // --- DELETE: ACCOUNT (Admin Only) ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsAdmin()) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (GetCurrentUserId() == id) return BadRequest("Self-deletion is blocked.");
            if (!await CanManageAsync(user)) return Forbid();

            var hasTasks = await _context.Tasks.AnyAsync(t => t.AssignedToUserID == id);
            if (hasTasks) return BadRequest("Reassign this user's tasks before deleting them.");

            await _userManager.DeleteAsync(user);
            return Ok();
        }

        // --- HELPERS ---
        private bool IsAdmin() => GetUserRole() == "1" || GetUserRole() == "4";

        private int CurrentRoleId() => int.TryParse(GetUserRole(), out var r) ? r : 0;

        // Owner > Admin > Foreman > Worker
        private static int RoleRank(int roleId) => roleId switch { 4 => 4, 1 => 3, 2 => 2, 3 => 1, _ => 0 };

        private async Task<bool> IsSameCompanyAsync(User target)
        {
            if (target.Id == GetCurrentUserId()) return true;
            var currentUser = await _userManager.FindByIdAsync(GetCurrentUserId()!);
            return currentUser?.CompanyId != null && currentUser.CompanyId == target.CompanyId;
        }

        // Yourself, or someone in your company with a lower rank than you
        private async Task<bool> CanManageAsync(User target)
        {
            if (target.Id == GetCurrentUserId()) return true;
            return await IsSameCompanyAsync(target) && RoleRank(target.RoleID) < RoleRank(CurrentRoleId());
        }
        private string? GetUserRole() => User.FindFirst("RoleID")?.Value;
        private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}