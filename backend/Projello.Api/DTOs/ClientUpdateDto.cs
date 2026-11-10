using System.ComponentModel.DataAnnotations;

namespace Projello.Api.DTOs;

public class ClientUpdateDto
{
    // Payments are edited per project (PUT /api/projects/{id}/payments)

    [MaxLength(50)]
    public string? Status { get; set; }
}
