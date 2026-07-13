using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Customers
{
    public class CustomerCreateDto
    {
        [Required][MaxLength(20)] public string SocialSecurityNumber { get; set; } = default!;
        [Required][MaxLength(100)] public string FirstName { get; set; } = default!;
        [Required][MaxLength(100)] public string LastName { get; set; } = default!;
        [Required] public DateTime DateOfBirth { get; set; }
        [Required][EmailAddress][MaxLength(200)] public string Email { get; set; } = default!;
        [Required][MaxLength(300)] public string Address1 { get; set; } = default!;
        [Required][MaxLength(20)] public string Phone { get; set; } = default!;
        [Required][AllowedValues("Permiso de trabajo", "Residente permanente", "Ciudadano", "Otro", "Asilo",
            ErrorMessage = "Estatus migratorio inválido.")]
        public string MigrationStatus { get; set; } = default!;

        [Required][AllowedValues("Cónyuge", "Hijo/a", "Madre", "Padre", "Sobrino/a", "Nieto/a", "Hijastro/a", "Hermano/a", "Otro",
            ErrorMessage = "Relación con el principal inválida.")]
        public string RelacionConPrincipal { get; set; } = default!;

        [MaxLength(10)] public string? ZipCode { get; set; }

        // Sin [AllowedValues]: es opcional (AllowedValues de .NET rechaza null
        // en vez de tratarlo como "sin validar"), y la lista ya está constreñida
        // por el <select> del frontend. Mismo criterio que County (§ más abajo).
        [MaxLength(2)] public string? State { get; set; }

        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(100)] public string? County { get; set; }
        [MaxLength(20)] public string? MaritalStatus { get; set; }

        [MaxLength(100)] public string? Occupation { get; set; }

        // Solo Administrador puede asignarlos; para el resto de los roles el
        // backend ignora estos valores y fuerza AgentId al usuario logueado.
        public int? AgentId { get; set; }
        public int? AssistantAgentId { get; set; }
        public int? RecordAgentId { get; set; }

        [MaxLength(100)] public string? MiddleName { get; set; }

        // Sin [AllowedValues]: mismo motivo que State/County más arriba (opcional,
        // AllowedValues rechaza null en vez de tratarlo como "sin validar").
        [MaxLength(20)] public string? Gender { get; set; }

        [MaxLength(50)] public string? GreenCard { get; set; }
        [MaxLength(50)] public string? WorkPermit { get; set; }
        [MaxLength(300)] public string? Address2 { get; set; }
        [MaxLength(150)] public string? EmployerName { get; set; }
        [MaxLength(20)] public string? CompanyPhone { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El ingreso anual no puede ser negativo.")]
        public decimal AnnualIncome { get; set; }

        [MaxLength(500)] public string? Tags { get; set; }

        // Idioma de preferencia de CONTACTO del cliente (no confundir con
        // User.PreferredLanguage, que es el idioma de la interfaz del usuario logueado).
        // Sin [AllowedValues] por el mismo motivo que Gender.
        [MaxLength(20)] public string? ContactLanguage { get; set; }
    }
}
