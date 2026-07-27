namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public List<DashboardNameCountDto> ByInsuranceCompany { get; set; } = new();
        public List<DashboardNameCountDto> ByCounty { get; set; } = new();
        public List<DashboardNameCountDto> ByCity { get; set; } = new();
    }
}
