using System.ComponentModel.DataAnnotations;

namespace NoteManagerApi.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }
    
    [Required]
    public string UserName { get; set; }
}