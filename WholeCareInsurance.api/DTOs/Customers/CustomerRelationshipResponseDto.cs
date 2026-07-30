namespace WholeCareInsurance.api.DTOs.Customers
{
    public class CustomerRelationshipResponseDto
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? RelationshipType { get; set; }
    }
}
