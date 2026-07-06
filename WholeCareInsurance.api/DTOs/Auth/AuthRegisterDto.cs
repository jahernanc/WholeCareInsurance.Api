using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Auth
{
    public class AuthRegisterDto
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = default!;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = default!;

        [Required]
        [AllowedValues("Admin", "Agente", ErrorMessage = "Rol inválido.")]
        public string Rol { get; set; } = default!;
    }
}
