namespace WholeCareInsurance.api.DTOs.Dashboard
{
    // Fila genérica "nombre -> cantidad de pólizas", reusada por aseguradora/condado/ciudad en §9.3.
    public class DashboardNameCountDto
    {
        public string Name { get; set; } = default!;
        public int PoliciesCount { get; set; }
    }
}
