using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; } = default!;

        [Required][MinLength(8)]
        public string NewPassword { get; set; } = default!;
    }
}
