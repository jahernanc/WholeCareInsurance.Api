namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardUpcoming65Dto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
    }
}
