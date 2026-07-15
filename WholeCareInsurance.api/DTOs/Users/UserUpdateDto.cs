using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Users
{
    public class UserUpdateDto
    {
        [Required][MaxLength(100)] public string Nombre { get; set; } = default!;
        [Required][EmailAddress][MaxLength(200)] public string Email { get; set; } = default!;

        [Required][AllowedValues("Admin", "Agente", ErrorMessage = "Rol inválido.")]
        public string Rol { get; set; } = default!;

        public bool IsEncargado { get; set; }

        [MaxLength(100)] public string? MiddleName { get; set; }
        [MaxLength(20)] public string? Gender { get; set; }

        [MaxLength(300)] public string? Address1 { get; set; }
        [MaxLength(300)] public string? Address2 { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(10)] public string? ZipCode { get; set; }
        [MaxLength(2)] public string? State { get; set; }
        [MaxLength(100)] public string? County { get; set; }

        public bool Licensed { get; set; }
        [MaxLength(50)] public string? LicenseNumber { get; set; }

        [MaxLength(50)] public string? NpnNumber { get; set; }
        public bool NpnOverride { get; set; }

        public bool HasCompanyContract { get; set; }
        [MaxLength(50)] public string? ContractNumber { get; set; }
        [MaxLength(150)] public string? CompanyName { get; set; }

        [MaxLength(200)] public string? ContractsWanted { get; set; }
        [MaxLength(1000)] public string? AdditionalInformation { get; set; }

        // No se re-valida como obligatorio en edición: si ya era true, el checkbox
        // requerido del frontend llega pre-marcado y esto solo persiste el valor tal cual.
        public bool TermsAccepted { get; set; }
    }
}
