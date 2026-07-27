namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardLatestPolicyDto
    {
        public int Id { get; set; }
        public string PolicyNumber { get; set; } = default!;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime UpdatedAt { get; set; }
    }
}
