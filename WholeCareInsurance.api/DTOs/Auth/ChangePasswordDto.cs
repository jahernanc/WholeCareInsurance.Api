using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = default!;

        [Required][MinLength(8)]
        public string NewPassword { get; set; } = default!;
    }
}
