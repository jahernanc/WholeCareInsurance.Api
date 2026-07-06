using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Auth
{
    public class AuthLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = default!;
    }
}
