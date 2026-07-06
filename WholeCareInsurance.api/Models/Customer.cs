namespace WholeCareInsurance.api.Models
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string Email { get; set; } = default!;

        public string DocumentNumber { get; set; } = default!; // DNI / ID

        // ✅ Relación 1-N
        public ICollection<Policy> Policies { get; set; } = new List<Policy>();

    }
}
