namespace WholeCareInsurance.api.DTOs.Users
{
    public class UserUpdateDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; } // Admin o Agente
    }
}
