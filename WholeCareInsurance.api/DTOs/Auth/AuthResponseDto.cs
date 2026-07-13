namespace WholeCareInsurance.api.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public string PreferredLanguage { get; set; } = default!;
        public bool MustChangePassword { get; set; }
    }
}
