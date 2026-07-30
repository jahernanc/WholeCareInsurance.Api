namespace WholeCareInsurance.api.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        // Null para un Agente (no tiene sentido escalado a una sola persona) — solo Admin las ve.
        public int? AgenciesCount { get; set; }
        public int? AgentsCount { get; set; }

        public int PoliciesCount { get; set; }
        public int MembersCount { get; set; }

        // Personas físicas distintas (titulares + dependientes vía CustomerRelationship,
        // deduplicado por Customer.Id) — a diferencia de MembersCount, no cuenta a la
        // misma persona dos veces si está cubierta en 2 pólizas del scope (§24.4).
        public int UniqueMembersCount { get; set; }
    }
}
