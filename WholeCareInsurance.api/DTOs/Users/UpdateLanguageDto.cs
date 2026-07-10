using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Users
{
    public class UpdateLanguageDto
    {
        [Required]
        [AllowedValues("en", "es", ErrorMessage = "Idioma inválido.")]
        public string Language { get; set; } = default!;
    }
}
