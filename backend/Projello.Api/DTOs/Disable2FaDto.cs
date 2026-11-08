using System.ComponentModel.DataAnnotations;

namespace Projello.Api.DTOs;

public class Disable2FaDto
{
    // A stolen token alone must not be enough to switch 2FA off
    [Required]
    public string Password { get; set; } = null!;
}
