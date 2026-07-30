using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Users
{
    // Usado por PUT /users/me (§26.2) — a propósito NO incluye Rol/IsEncargado/IsActive
    // (organizacionales, Admin-only vía PUT /users/{id}) ni Agency (asignación
    // organizacional, solo lectura en el perfil propio): que no existan como propiedades
    // acá es lo que garantiza que no se puedan pisar aunque alguien arme el body a mano.
    public class UserProfileUpdateDto
    {
        [Required][MaxLength(100)] public string Nombre { get; set; } = default!;
        [Required][EmailAddress][MaxLength(200)] public string Email { get; set; } = default!;

        [MaxLength(100)] public string? MiddleName { get; set; }
        [MaxLength(20)] public string? Gender { get; set; }
        [MaxLength(20)] public string? Phone { get; set; }

        [Required][MaxLength(300)] public string Address1 { get; set; } = default!;
        [MaxLength(300)] public string? Address2 { get; set; }
        [Required][MaxLength(100)] public string City { get; set; } = default!;
        [Required][MaxLength(10)] public string ZipCode { get; set; } = default!;
        [Required][MaxLength(2)] public string State { get; set; } = default!;
        [Required][MaxLength(100)] public string County { get; set; } = default!;

        // Solo obligatoria cuando Email difiere del valor actual del usuario — validado
        // en el controller (mismo criterio que POST /auth/change-password).
        public string? CurrentPassword { get; set; }
    }
}
