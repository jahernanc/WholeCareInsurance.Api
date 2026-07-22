using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface IPolicyHistoryService
    {
        Task<List<PolicyHistory>> GetForPolicy(int policyId);

        // Registra un cambio de Status (alta: oldStatus null; edición: oldStatus/newStatus
        // distintos). No hace nada si oldStatus == newStatus.
        Task RecordStatusChange(int policyId, string? oldStatus, string newStatus, int? changedByUserId, string source = "Sistema");

        // Pensado para que el futuro script de migración lo invoque directamente
        // (sin pasar por un endpoint HTTP) para cargar snapshots históricos en bloque.
        Task AddBulk(IEnumerable<PolicyHistory> entries);
    }
}
