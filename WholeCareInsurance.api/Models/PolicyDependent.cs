namespace WholeCareInsurance.api.Models
{
    public class PolicyDependent
    {
        public int PolicyId { get; set; }
        public Policy Policy { get; set; } = default!;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;
    }
}
