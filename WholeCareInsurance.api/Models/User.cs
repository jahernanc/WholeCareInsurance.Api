namespace WholeCareInsurance.api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Rol { get; set; } = default!;
        public bool IsEncargado { get; set; }
        public string PreferredLanguage { get; set; } = "en";

        // ✅ Refresh token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public string? RefreshTokenHash { get; set; }

        public bool MustChangePassword { get; set; }
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

    }
}
