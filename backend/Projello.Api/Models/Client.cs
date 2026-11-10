using System.ComponentModel.DataAnnotations;

namespace Projello.Api.Models;

public class Client
{
    [Key]
    public int ClientID { get; set; }
    
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;
    
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
    
    [MaxLength(30)]
    public string? ContactPhone { get; set; }
    
    public bool IsBlacklisted { get; set; } = false;
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //  Company Scoping 
    [Required]
    public int CompanyID { get; set; }
    public Company? Company { get; set; }
    

    public ICollection<Project> Projects { get; set; } = new List<Project>();

    // Blacklisting fields
    [MaxLength(500)]
    public string? BlacklistReason { get; set; }

    public DateTime? BlacklistedAt { get; set; }

    public string? BlacklistedById { get; set; }
    public User? BlacklistedBy { get; set; }

    // updating
        public string Status { get; set; } = "Active";
        // Payments live on projects; a client's totals are the sum of its projects

    // Soft delete: a deleted record is hidden everywhere but can be restored
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }

   
}