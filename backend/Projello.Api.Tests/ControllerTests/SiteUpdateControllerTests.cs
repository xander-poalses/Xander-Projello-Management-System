using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projello.Api.Controllers;
using Projello.Api.Data;
using Projello.Api.DTOs;
using Projello.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Projello.Api.Tests.Controllers
{
    public class SiteUpdatesControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SiteUpdatesController _controller;
        private readonly Cloudinary _dummyCloudinary;

        public SiteUpdatesControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var account = new Account("dummy_cloud", "dummy_key", "dummy_secret");
            _dummyCloudinary = new Cloudinary(account);

            _controller = new SiteUpdatesController(_context, _dummyCloudinary);

            // Default to an Admin so mapping tests aren't blocked by the project access check
            SetCurrentUser("test-admin", "1");
        }

        // Puts the user in the test company as a member of the project, creating the
        // project and any missing posts so access checks pass
        private void GrantAccess(string userId, int projectId, params int[] updateIds)
        {
            if (!_context.Projects.Any(p => p.ProjectID == projectId))
                _context.Projects.Add(new Project { ProjectID = projectId, Name = "Site", Status = "Active", CreatedByUserID = userId });
            _context.ProjectMembers.Add(new ProjectMember { ProjectID = projectId, UserID = userId, AssignedAs = "Worker" });
            foreach (var id in updateIds.Where(id => !_context.ProjectUpdates.Any(u => u.Id == id)))
                _context.ProjectUpdates.Add(new ProjectUpdate { Id = id, ProjectId = projectId, UserId = userId, ImageUrl = "" });
            _context.SaveChanges();
            TestCompany.Adopt(_context, userId);
        }

        // Creates a company, client and project, and adds the user to it as a worker
        private void SeedProjectMember(int projectId, string userId, bool addMembership = true)
        {
            var company = new Company { CompanyID = 1, Name = "Test Co", CreatedAt = DateTime.UtcNow };
            _context.Companies.Add(company);
            _context.Users.Add(new User { Id = userId, FullName = "Site Worker", Email = $"{userId}@test.com", CompanyId = 1 });
            _context.Clients.Add(new Client { ClientID = 1, Name = "Test Client", CompanyID = 1 });
            _context.Projects.Add(new Project { ProjectID = projectId, ClientID = 1, Name = "Test Project", Status = "Active", CreatedByUserID = userId });
            if (addMembership)
                _context.ProjectMembers.Add(new ProjectMember { ProjectID = projectId, UserID = userId, AssignedAs = "Worker" });
            _context.SaveChanges();
        }

        private void SetCurrentUser(string userId, string roleId = null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (!string.IsNullOrEmpty(roleId))
            {
                claims.Add(new Claim("RoleID", roleId));
            }

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetUpdates_ReturnsMappedUpdatesWithReactionsAndComments()
        {

            var projectId = 1;
            var userId = "user-123";

            _context.Users.Add(new User { Id = userId, FullName = "John Doe" });

            var update = new ProjectUpdate
            {
                Id = 10,
                ProjectId = projectId,
                UserId = userId,
                Caption = "Site poured today",
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectUpdates.Add(update);

            _context.UpdateReactions.Add(new UpdateReaction { Id = 1, UpdateId = 10, UserId = userId, Emoji = "👍" });
            _context.UpdateComments.Add(new UpdateComment { Id = 1, UpdateId = 10, UserId = userId, CommentText = "Looks good!" });

            await _context.SaveChangesAsync();

            GrantAccess("test-admin", projectId);
            var result = await _controller.GetUpdates(projectId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsAssignableFrom<IEnumerable<ProjectDiscussionPostDto>>(okResult.Value);
            
            var posts = payload.ToList();
            Assert.Single(posts);
            
            var post = posts.First();
            Assert.Equal("John Doe", post.UserFullName);
            Assert.Equal("Site poured today", post.Caption);
            
            Assert.Single(post.Reactions);
            Assert.Equal("👍", post.Reactions.First().Emoji);
            Assert.Equal("John Doe", post.Reactions.First().UserFullName);

            Assert.Single(post.Comments);
            Assert.Equal("Looks good!", post.Comments.First().CommentText);
        }

        [Fact]
        public async Task GetUpdates_UserMapFallback_ReturnsUnknownUserString()
        {

            var projectId = 5;
            var update = new ProjectUpdate { Id = 50, ProjectId = projectId, UserId = "ghost-user", Caption = "Ghost update" };
            _context.ProjectUpdates.Add(update);
            _context.UpdateReactions.Add(new UpdateReaction { Id = 5, UpdateId = 50, UserId = "ghost-user", Emoji = "😮" });
            _context.UpdateComments.Add(new UpdateComment { Id = 5, UpdateId = 50, UserId = "ghost-user", CommentText = "Ghost comment" });
            await _context.SaveChangesAsync();

            GrantAccess("test-admin", projectId);
            var result = await _controller.GetUpdates(projectId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var posts = Assert.IsAssignableFrom<IEnumerable<ProjectDiscussionPostDto>>(okResult.Value).ToList();
            
            Assert.Equal("Unknown user", posts.First().UserFullName);
            Assert.Equal("Unknown user", posts.First().Reactions.First().UserFullName);
            Assert.Equal("Unknown user", posts.First().Comments.First().UserFullName);
        }

        [Fact]
        public async Task CreateUpdate_WithoutImage_CreatesAndReturnsUpdate()
        {

            var userId = "user-123";
            SeedProjectMember(1, userId);
            SetCurrentUser(userId, "3");

            var dto = new CreateUpdateDto
            {
                Caption = "Progress check",
                Image = null
            };

            var result = await _controller.CreateUpdate(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdUpdate = Assert.IsType<ProjectUpdate>(okResult.Value);

            Assert.Equal("Progress check", createdUpdate.Caption);
            Assert.Equal(string.Empty, createdUpdate.ImageUrl);
            Assert.Equal(userId, createdUpdate.UserId);
            Assert.Equal(1, _context.ProjectUpdates.Count());
        }

        [Fact]
        public async Task GetUpdates_WorkerNotOnProject_ReturnsForbid()
        {
            SeedProjectMember(1, "outsider", addMembership: false);
            SetCurrentUser("outsider", "3");

            var result = await _controller.GetUpdates(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetUpdates_WorkerOnProject_ReturnsOk()
        {
            SeedProjectMember(1, "member");
            SetCurrentUser("member", "3");

            var result = await _controller.GetUpdates(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task React_TogglesReaction_AddsThenRemoves()
        {

            var userId = "user-123";
            SetCurrentUser(userId);
            var updateId = 10;
            var emoji = "🔥";

            var dto = new ReactDto { Emoji = emoji };

            GrantAccess(userId, 1, updateId);
            var addResult = await _controller.React(updateId, dto);
            
            var addOkResult = Assert.IsType<OkObjectResult>(addResult);
            var addResponse = addOkResult.Value?.GetType().GetProperty("removed")?.GetValue(addOkResult.Value, null);
            Assert.Equal(false, addResponse);
            Assert.Single(_context.UpdateReactions);

            var removeResult = await _controller.React(updateId, dto);

            var removeOkResult = Assert.IsType<OkObjectResult>(removeResult);
            var removeResponse = removeOkResult.Value?.GetType().GetProperty("removed")?.GetValue(removeOkResult.Value, null);
            Assert.Equal(true, removeResponse);
            Assert.Empty(_context.UpdateReactions);
        }

        [Fact]
        public async Task AddComment_ValidData_AddsCommentToDatabase()
        {

            var userId = "user-123";
            SetCurrentUser(userId);
            var updateId = 10;

            var dto = new CommentDto { CommentText = "Great progress!" };

            GrantAccess(userId, 1, updateId);
            var result = await _controller.AddComment(updateId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var comment = Assert.IsType<UpdateComment>(okResult.Value);

            Assert.Equal("Great progress!", comment.CommentText);
            Assert.Equal(userId, comment.UserId);
            Assert.Equal(updateId, comment.UpdateId);
        }

        // ==================== DELETE UPDATE BRANCH TESTS ====================

        [Fact]
        public async Task DeleteUpdate_NotFound_ReturnsNotFound()
        {

            SetCurrentUser("any-user");

            var result = await _controller.DeleteUpdate(99999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Update not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteUpdate_NotAuthorAndNotAdmin_ReturnsForbid()
        {

            var update = new ProjectUpdate { Id = 20, ProjectId = 1, UserId = "user-A", Caption = "Secure Post" };
            _context.ProjectUpdates.Add(update);
            await _context.SaveChangesAsync();

            SetCurrentUser("user-B", "3");

            var result = await _controller.DeleteUpdate(20);

            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteUpdate_IsAuthor_DeletesSuccessfully()
        {

            var update = new ProjectUpdate { Id = 21, ProjectId = 1, UserId = "author-user", Caption = "My Post" };
            _context.ProjectUpdates.Add(update);
            await _context.SaveChangesAsync();

            SetCurrentUser("author-user", "3");

            var result = await _controller.DeleteUpdate(21);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.ProjectUpdates.FindAsync(21));
        }

        [Fact]
        public async Task DeleteUpdate_IsAdmin_DeletesSuccessfully()
        {

            var update = new ProjectUpdate { Id = 22, ProjectId = 1, UserId = "regular-user", Caption = "User Post" };
            _context.ProjectUpdates.Add(update);
            await _context.SaveChangesAsync();

            SetCurrentUser("admin-user", "1");

            GrantAccess("admin-user", 1);
            var result = await _controller.DeleteUpdate(22);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.ProjectUpdates.FindAsync(22));
        }

        // ==================== DELETE COMMENT BRANCH TESTS ====================

        [Fact]
        public async Task DeleteComment_NotFound_ReturnsNotFound()
        {

            SetCurrentUser("any-user");

            var result = await _controller.DeleteComment(1, 99999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Comment not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteComment_NotAuthorAndNotAdmin_ReturnsForbid()
        {

            var comment = new UpdateComment { Id = 30, UpdateId = 1, UserId = "user-A", CommentText = "Nice" };
            _context.UpdateComments.Add(comment);
            await _context.SaveChangesAsync();

            SetCurrentUser("user-B", "3");

            var result = await _controller.DeleteComment(1, 30);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteComment_IsAuthor_DeletesSuccessfully()
        {

            var comment = new UpdateComment { Id = 31, UpdateId = 2, UserId = "comment-author", CommentText = "My Comment" };
            _context.UpdateComments.Add(comment);
            await _context.SaveChangesAsync();

            SetCurrentUser("comment-author", "3");

            var result = await _controller.DeleteComment(2, 31);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.UpdateComments.FindAsync(31));
        }

        [Fact]
        public async Task DeleteComment_IsOwnerRole_DeletesSuccessfully()
        {
            var comment = new UpdateComment { Id = 32, UpdateId = 2, UserId = "regular-user", CommentText = "User Comment" };
            _context.UpdateComments.Add(comment);
            await _context.SaveChangesAsync();

            SetCurrentUser("owner-user", "4");

            GrantAccess("owner-user", 1, 2);
            var result = await _controller.DeleteComment(2, 32);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.UpdateComments.FindAsync(32));
        }

        // ==================== GENERAL AUTHENTICATION TESTS ====================

        [Fact]
        public async Task UnauthenticatedUser_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var createResult = await _controller.CreateUpdate(1, new CreateUpdateDto());
            var reactResult = await _controller.React(1, new ReactDto());
            var commentResult = await _controller.AddComment(1, new CommentDto());
            var deleteUpdateResult = await _controller.DeleteUpdate(1);
            var deleteCommentResult = await _controller.DeleteComment(1, 1);

            Assert.IsType<UnauthorizedResult>(createResult);
            Assert.IsType<UnauthorizedResult>(reactResult);
            Assert.IsType<UnauthorizedResult>(commentResult);
            Assert.IsType<UnauthorizedResult>(deleteUpdateResult);
            Assert.IsType<UnauthorizedResult>(deleteCommentResult);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}