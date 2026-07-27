using WholeCareInsurance.api.DTOs.Dashboard;

namespace WholeCareInsurance.api.Services
{
    // Todos los métodos aceptan agentId ya resuelto por el controller (ResolveEffectiveAgentId):
    // null = vista global (solo posible para Admin), un Id concreto = scopeado a ese agente
    // (Agente autenticado siempre pasa el suyo, Admin puede pasar cualquiera para "pararse en
    // los zapatos" de un agente puntual). El service no vuelve a decidir el rol, solo filtra.
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummary(int? agentId, DateTime? from, DateTime? to);
        Task<List<DashboardStatusCountDto>> GetByStatus(int? agentId, DateTime? from, DateTime? to);
        Task<List<DashboardTypeCountDto>> GetByType(int? agentId, DateTime? from, DateTime? to);
        Task<DashboardStatsDto> GetStats(int? agentId, DateTime? from, DateTime? to);
        Task<List<DashboardLatestPolicyDto>> GetLatestPolicies(int? agentId);
        Task<List<DashboardUpcoming65Dto>> GetUpcoming65(int? agentId);
    }
}
