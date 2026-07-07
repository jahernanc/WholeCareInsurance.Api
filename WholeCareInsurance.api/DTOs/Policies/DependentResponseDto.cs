namespace WholeCareInsurance.api.DTOs.Policies
{
    public class DependentResponseDto
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string SocialSecurityNumber { get; set; } = default!;
    }
}
