using System.ComponentModel.DataAnnotations;

namespace Projello.Api.DTOs;

// What has been paid and what is still owed on one project (Rand)
public class ProjectPaymentsDto
{
    [Range(0, 999_999_999_999)]
    public decimal TotalPaid { get; set; }

    [Range(0, 999_999_999_999)]
    public decimal Outstanding { get; set; }
}
