namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardStatusCountDto
    {
        public string Status { get; set; } = default!;
        public int PoliciesCount { get; set; }
        public int MembersCount { get; set; }
    }
}
