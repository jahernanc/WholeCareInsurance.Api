namespace WholeCareInsurance.api.DTOs.Users
{
    public class UserCreateDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Rol { get; set; } // Admin o Agente
        public bool IsEncargado { get; set; }
    }
}
