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
        [Required][MaxLength(300)] public string Address { get; set; } = default!;
        [Required][MaxLength(20)] public string Phone { get; set; } = default!;
        [Required][AllowedValues("Permiso de trabajo", "Residente permanente", "Ciudadano", "Otro",
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
    }
}
