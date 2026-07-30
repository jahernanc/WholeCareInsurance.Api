namespace WholeCareInsurance.api.DTOs.Users
{
    // Extendido (§26) con los campos de perfil editable (Profile.jsx) además de los
    // ya usados por la reconciliación en background de AppLayout.jsx (PreferredLanguage,
    // MustChangePassword) — un solo endpoint GET /users/me para ambos usos, en vez de
    // agregar uno nuevo, porque AppLayout.jsx solo lee esos 2 campos y no le afecta que
    // el DTO tenga más. Agency se expone (de solo lectura en el frontend) pero no forma
    // parte de UserProfileUpdateDto — no es editable desde PUT /users/me (§26.2).
    public class UserMeDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool IsEncargado { get; set; }
        public string PreferredLanguage { get; set; }
        public bool MustChangePassword { get; set; }

        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Agency { get; set; }
        public string Address1 { get; set; }
        public string? Address2 { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string State { get; set; }
        public string County { get; set; }
    }
}
