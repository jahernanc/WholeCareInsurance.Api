namespace WholeCareInsurance.api.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool IsEncargado { get; set; }
        public string PreferredLanguage { get; set; }

        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? State { get; set; }
        public string? County { get; set; }

        public bool Licensed { get; set; }
        public string? LicenseNumber { get; set; }

        public string? NpnNumber { get; set; }
        public bool NpnOverride { get; set; }

        public bool HasCompanyContract { get; set; }
        public string? ContractNumber { get; set; }
        public string? CompanyName { get; set; }

        public string? ContractsWanted { get; set; }
        public string? AdditionalInformation { get; set; }

        public bool TermsAccepted { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
    }
}
