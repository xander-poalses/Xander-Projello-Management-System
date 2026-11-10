using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Projello.Api.Models;
using Projello.Api.Data; 
namespace Projello.Api.Data
{
    // They cannot be found until code is added in the Models folder for it.
    public class AppDbContext : IdentityDbContext<User> // Base class for Identity tables + custom User
    {
                private readonly IHttpContextAccessor? _http;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { } // Injects DB connection from Program.cs

        // Used by the app: knows who is signed in, for the audit trail
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor http)
            : base(options)
        {
            _http = http;
        }

        public DbSet<Project> Projects { get; set; } = null!; // Defines tables for EF queries
        public DbSet<Milestone> Milestones { get; set; } = null!;
        public DbSet<TaskItem> Tasks { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
        public DbSet<ProgressUpdate> ProgressUpdates { get; set; } = null!;
        public DbSet<Reaction> Reactions { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;

        public DbSet<Company> Companies { get; set; } = null!;

        public DbSet<CompanyInvite> CompanyInvites { get; set; } = null!;

        public DbSet<ProjectUpdate> ProjectUpdates { get; set; } = null!;
        public DbSet<UpdateReaction> UpdateReactions { get; set; } = null!;
                public DbSet<UpdateComment> UpdateComments { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;



        //Section below is extra rule for the database, This one is so no client can be blacklisted by default, they have to be manually blacklisted by an admin.
        protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

            // No client is blacklisted by default
            builder.Entity<Client>()
                .Property(c => c.IsBlacklisted)
                .HasDefaultValue(false);

            // Correct relationship configuration
            builder.Entity<Client>()
                .HasOne(c => c.BlacklistedBy)           // Navigation property (User)
                .WithMany()                             // Admin can blacklist many clients
                .HasForeignKey(c => c.BlacklistedById)  // The FK property (string)
                .OnDelete(DeleteBehavior.SetNull);      // If admin is deleted, keep the client record

            // Company relationship configuration
            builder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Company Invite relationship
            builder.Entity<CompanyInvite>()
                .HasOne(i => i.Company)
                .WithMany()
                .HasForeignKey(i => i.CompanyID)
                .OnDelete(DeleteBehavior.Cascade);

                                    // Soft-deleted clients and projects are hidden from every query.
            // Use IgnoreQueryFilters() to list or restore them.
            builder.Entity<Client>().HasQueryFilter(c => c.DeletedAt == null);
            builder.Entity<Project>().HasQueryFilter(p => p.DeletedAt == null);

            builder.Entity<AuditLog>().HasIndex(a => new { a.CompanyId, a.CreatedAt });
            builder.Entity<AuditLog>().Property(a => a.Summary).HasMaxLength(500);

            // Money: two decimals, enough digits for any project
            builder.Entity<Project>().Property(p => p.TotalPaid).HasPrecision(18, 2);
            builder.Entity<Project>().Property(p => p.Outstanding).HasPrecision(18, 2);

            // Invite codes are looked up by value, so they must be unique
            builder.Entity<CompanyInvite>()
                .HasIndex(i => i.Code)
                .IsUnique();

            // Deleting a client or company must never silently wipe projects:
            // move or delete the projects first (was Cascade)
            builder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Client>()
                .HasOne(c => c.Company)
                .WithMany()
                .HasForeignKey(c => c.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Discussion posts, reactions and comments had no foreign keys, so deleting a
            // project or user left orphaned rows. They now belong to their project/post/user.
            builder.Entity<ProjectUpdate>()
                .HasOne<Project>()
                .WithMany()
                .HasForeignKey(u => u.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectUpdate>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UpdateReaction>()
                .HasOne<ProjectUpdate>()
                .WithMany()
                .HasForeignKey(r => r.UpdateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UpdateReaction>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UpdateComment>()
                .HasOne<ProjectUpdate>()
                .WithMany()
                .HasForeignKey(c => c.UpdateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UpdateComment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // --- AUDIT TRAIL ---
        // Every change to a client, project, milestone or task is recorded with who made it
        private static readonly HashSet<Type> Audited = new() { typeof(Client), typeof(Project), typeof(Milestone), typeof(TaskItem) };

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => Audited.Contains(e.Entity.GetType()) &&
                            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0)
                return await base.SaveChangesAsync(cancellationToken);

            var userId = _http?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = _http?.HttpContext?.User.FindFirstValue("FullName");
            int? companyId = userId == null ? null
                : await Users.Where(u => u.Id == userId).Select(u => u.CompanyId).FirstOrDefaultAsync(cancellationToken);

            // Describe the changes before saving (afterwards, added rows have their ids)
            var pending = entries.Select(e => (Entry: e, Action: DescribeAction(e), Summary: DescribeChange(e))).ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var (entry, action, summary) in pending)
            {
                AuditLogs.Add(new AuditLog
                {
                    CompanyId = companyId,
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityType = entry.Entity.GetType().Name == "TaskItem" ? "Task" : entry.Entity.GetType().Name,
                    EntityId = string.Join(",", entry.Metadata.FindPrimaryKey()!.Properties.Select(pk => entry.Property(pk.Name).CurrentValue)),
                    Summary = summary.Length > 500 ? summary[..500] : summary,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            await base.SaveChangesAsync(cancellationToken);
            return result;
        }

        private static string DescribeAction(EntityEntry e)
        {
            if (e.State == EntityState.Added) return "Created";
            if (e.State == EntityState.Deleted) return "Deleted";
            var deletedAt = e.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
            if (deletedAt is { IsModified: true })
                return deletedAt.CurrentValue == null ? "Restored" : "Deleted";
            return "Updated";
        }

        private static string DescribeChange(EntityEntry e)
        {
            var name = e.Properties.FirstOrDefault(p => p.Metadata.Name is "Name" or "Title")?.CurrentValue?.ToString();
            if (e.State != EntityState.Modified) return name ?? "";
            var changed = e.Properties
                .Where(p => p.IsModified && p.Metadata.Name is not ("DeletedAt" or "DeletedById")
                            && !Equals(p.OriginalValue, p.CurrentValue))
                .Select(p => $"{p.Metadata.Name}: {p.OriginalValue ?? "empty"} to {p.CurrentValue ?? "empty"}");
            var details = string.Join("; ", changed);
            return string.IsNullOrEmpty(details) ? name ?? "" : $"{name}: {details}";
        }
    }
}