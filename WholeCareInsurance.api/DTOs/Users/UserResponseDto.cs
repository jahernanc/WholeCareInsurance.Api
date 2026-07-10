namespace WholeCareInsurance.api.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool IsEncargado { get; set; }
    }
}
