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

        // Datos de perfil del Agente (§11) — EE.UU.-only, mismo criterio que Customer.
        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string Address1 { get; set; } = default!;
        public string? Address2 { get; set; }
        public string City { get; set; } = default!;
        public string ZipCode { get; set; } = default!;
        public string State { get; set; } = default!;
        public string County { get; set; } = default!;

        public bool Licensed { get; set; }
        public string? LicenseNumber { get; set; }

        public string? NpnNumber { get; set; }
        public bool NpnOverride { get; set; }

        public bool HasCompanyContract { get; set; }
        public string? ContractNumber { get; set; }
        public string? CompanyName { get; set; }

        // Comma-separated: subconjunto de Medicare/Obamacare/Supplemental Plans/Life Insurance.
        public string? ContractsWanted { get; set; }

        public string? AdditionalInformation { get; set; }

        public bool TermsAccepted { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }

        // ✅ Refresh token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public string? RefreshTokenHash { get; set; }

        public bool MustChangePassword { get; set; }
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

    }
}
