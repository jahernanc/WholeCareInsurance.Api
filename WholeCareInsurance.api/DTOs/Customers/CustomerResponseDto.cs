namespace WholeCareInsurance.api.DTOs.Customers
{
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public string SocialSecurityNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = default!;
        public string Address1 { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string MigrationStatus { get; set; } = default!;
        public string RelacionConPrincipal { get; set; } = default!;

        public string? ZipCode { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Occupation { get; set; }

        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string? GreenCard { get; set; }
        public string? WorkPermit { get; set; }
        public string? Address2 { get; set; }
        public string? EmployerName { get; set; }
        public string? CompanyPhone { get; set; }
        public decimal AnnualIncome { get; set; }
        public string? Tags { get; set; }
        public string? ContactLanguage { get; set; }

        public int? Age { get; set; }
        public string? CountryOfBirth { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public bool? BackDateToSaveAge { get; set; }
        public bool? SpentMoreThan4MonthsAbroad { get; set; }
        public bool? MilitaryOrganizationMember { get; set; }
        public bool? CurrentlyEmployed { get; set; }
        public bool? HasDriverLicense { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public decimal? NetWorth { get; set; }
        public decimal? HouseholdIncome { get; set; }
        public decimal? HouseholdNetWorth { get; set; }

        public int? AgentId { get; set; }
        public string? AgentName { get; set; }
        public int? AssistantAgentId { get; set; }
        public string? AssistantAgentName { get; set; }
        public int? RecordAgentId { get; set; }
        public string? RecordAgentName { get; set; }

        public int PoliciesCount { get; set; }
    }
}
