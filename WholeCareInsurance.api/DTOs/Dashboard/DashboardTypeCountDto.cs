namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardTypeCountDto
    {
        public string Type { get; set; } = default!;
        public int PoliciesCount { get; set; }
    }
}
