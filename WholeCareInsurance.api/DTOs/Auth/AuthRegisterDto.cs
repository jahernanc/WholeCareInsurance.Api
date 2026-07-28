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

        public bool IsEncargado { get; set; }

        [MaxLength(100)] public string? MiddleName { get; set; }

        // Sin [AllowedValues]: es opcional (AllowedValues rechaza null en vez de
        // tratarlo como "sin validar"), mismo criterio que Customer (§3.2).
        [MaxLength(20)] public string? Gender { get; set; }
        [MaxLength(20)] public string? Phone { get; set; }

        // Agencia real del sistema anterior (§15.1) — a diferencia de Gender, sí se valida
        // contra los 2 valores confirmados (no se espera que crezca), incluyendo null
        // explícito en la lista para que siga siendo opcional.
        [AllowedValues(null, "Whole Care Insurance Group llC", "Preventive Health Insurance", ErrorMessage = "Agencia inválida.")]
        [MaxLength(150)]
        public string? Agency { get; set; }

        [Required][MaxLength(300)] public string Address1 { get; set; } = default!;
        [MaxLength(300)] public string? Address2 { get; set; }
        [Required][MaxLength(100)] public string City { get; set; } = default!;
        [Required][MaxLength(10)] public string ZipCode { get; set; } = default!;
        [Required][MaxLength(2)] public string State { get; set; } = default!;
        [Required][MaxLength(100)] public string County { get; set; } = default!;

        public bool Licensed { get; set; }
        [MaxLength(50)] public string? LicenseNumber { get; set; }

        [MaxLength(50)] public string? NpnNumber { get; set; }
        public bool NpnOverride { get; set; }

        public bool HasCompanyContract { get; set; }
        [MaxLength(50)] public string? ContractNumber { get; set; }
        [MaxLength(150)] public string? CompanyName { get; set; }

        [MaxLength(200)] public string? ContractsWanted { get; set; }
        [MaxLength(1000)] public string? AdditionalInformation { get; set; }

        // Validado explícitamente en el controller (debe ser true para poder registrar).
        public bool TermsAccepted { get; set; }
    }
}
