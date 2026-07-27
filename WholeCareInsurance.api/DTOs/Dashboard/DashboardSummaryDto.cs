namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        // Null para un Agente (no tiene sentido escalado a una sola persona) — solo Admin las ve.
        public int? AgenciesCount { get; set; }
        public int? AgentsCount { get; set; }

        public int PoliciesCount { get; set; }
        public int MembersCount { get; set; }
    }
}
