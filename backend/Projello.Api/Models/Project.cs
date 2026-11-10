using System.ComponentModel.DataAnnotations;

namespace Projello.Api.Models;

public class Project
{
    [Key]
    public int ProjectID { get; set; }
    [Required]
    public int ClientID { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    [MaxLength(30)]
    public string Status { get; set; } = "Planning";
    [Required]
    public string CreatedByUserID { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Client Client { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

        // The one place payments are stored (clients add these up). Rand, 2 decimals.
    public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }

    // Soft delete: a deleted record is hidden everywhere but can be restored
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
  
}