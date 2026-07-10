namespace WholeCareInsurance.api.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string SocialSecurityNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string MigrationStatus { get; set; } = default!;
        public string RelacionConPrincipal { get; set; } = default!;

        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    }
}
