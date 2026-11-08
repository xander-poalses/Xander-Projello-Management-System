using System.ComponentModel.DataAnnotations;

namespace Projello.Api.DTOs;

public class Verify2FaDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    // Issued by login when 2FA is on; required to finish a 2FA login
    public string? ChallengeToken { get; set; }
}