using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface ICustomerService
    {
        // role: "titular" (tiene al menos una Policy propia) | "dependiente" (tiene al
        // menos una fila como DependentCustomerId en CustomerRelationship) | null = sin
        // filtrar. Los dos roles no son excluyentes (§24.3) — un Customer puede ser ambos.
        Task<IEnumerable<Customer>> GetAll(string? role = null);
        Task<(List<Customer> Items, int TotalCount)> Search(int page, int pageSize, string? role = null);
        Task<Customer?> GetById(int id);
        Task<Customer> Create(Customer customer);
        Task<Customer> Update(Customer customer);
        Task Delete(Customer customer);

        Task<List<CustomerRelationship>> GetDependentsOf(int titularCustomerId);
        Task<List<CustomerRelationship>> GetTitularesOf(int dependentCustomerId);
        Task UpsertRelationship(int titularCustomerId, int dependentCustomerId, string? relationshipType);

        // Valores distintos de City ya cargados (§22.4: 191 tras la normalización) —
        // alimenta el <datalist> de autocomplete del frontend (§22.3, freno al input libre).
        Task<List<string>> GetDistinctCities();
    }
}
