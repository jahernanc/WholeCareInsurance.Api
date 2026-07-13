using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required][EmailAddress]
        public string Email { get; set; } = default!;
    }
}
