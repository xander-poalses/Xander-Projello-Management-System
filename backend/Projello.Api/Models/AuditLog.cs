namespace Projello.Api.Models;

// One row per change to a key record: who did what to which client, project, milestone or task
public class AuditLog
{
    public long Id { get; set; }
    public int? CompanyId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    /// <summary>Created, Updated, Deleted or Restored</summary>
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    /// <summary>Plain-language summary, for example "Renamed to Block B"</summary>
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
