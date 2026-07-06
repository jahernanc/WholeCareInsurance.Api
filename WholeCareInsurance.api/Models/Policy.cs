namespace WholeCareInsurance.api.Models
{
    public class Policy
    {

        public int Id { get; set; }

        public string PolicyNumber { get; set; } = default!;

        public string Type { get; set; } = default!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal Premium { get; set; }

        public string Status { get; set; } = default!;

        // ✅ FK
        public int CustomerId { get; set; }

        // ✅ navegación
        public Customer Customer { get; set; } = default!;

    }
}
