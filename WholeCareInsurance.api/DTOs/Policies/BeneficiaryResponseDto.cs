namespace WholeCareInsurance.api.DTOs.Policies
{
    public class BeneficiaryResponseDto
    {
        public int Id { get; set; }
        public string TypeOfRelationship { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? SocialSecurityNumber { get; set; }
    }
}
