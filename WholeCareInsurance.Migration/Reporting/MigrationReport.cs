using System.Text.Json;
using System.Text.Json.Serialization;

namespace WholeCareInsurance.Migration.Reporting
{
    public class MigrationReport
    {
        public string Mode { get; set; } = "dry-run";
        public DateTime RunAtUtc { get; set; } = DateTime.UtcNow;
        public int HistoryWindowDays { get; set; }

        public Dictionary<string, int> PoliciesCreatedByType { get; } = new();
        public Dictionary<string, int> PoliciesSkippedAlreadyMigratedByType { get; } = new();
        public int HistoryGroupsConsolidated { get; set; }
        public int HistoryEntriesCreated { get; set; }

        public int CustomersCreated { get; set; }
        public int CustomersMatchedBySsn { get; set; }
        public int CustomersMatchedByNameAndDob { get; set; }

        public List<string> InsuranceCompaniesCreated { get; } = new();

        public List<AgentFallbackEntry> AgentFallbacks { get; } = new();
        public List<string> MissingDataWarnings { get; } = new();
        public List<string> UnprocessableRows { get; } = new();
        public List<string> RelationConflicts { get; } = new();
        public List<string> SsnCollisionWarnings { get; } = new();
        public List<PolicyGroupSummary> PolicyGroups { get; } = new();
        public List<SkippedDuplicateEntry> SkippedDuplicatePolicyNumbers { get; } = new();

        public void AddPolicyCreated(string type)
            => PoliciesCreatedByType[type] = PoliciesCreatedByType.GetValueOrDefault(type) + 1;

        public void AddPolicySkipped(string type)
            => PoliciesSkippedAlreadyMigratedByType[type] = PoliciesSkippedAlreadyMigratedByType.GetValueOrDefault(type) + 1;

        public void Print()
        {
            Console.WriteLine();
            Console.WriteLine("========== REPORTE DE MIGRACIÓN ==========");
            Console.WriteLine($"Modo: {Mode}   Ventana de historial: {HistoryWindowDays} días");
            Console.WriteLine();
            Console.WriteLine("Policies creadas por tipo:");
            foreach (var (type, count) in PoliciesCreatedByType) Console.WriteLine($"  {type}: {count}");
            Console.WriteLine("Policies salteadas (ya migradas, PolicyNumber existente):");
            foreach (var (type, count) in PoliciesSkippedAlreadyMigratedByType) Console.WriteLine($"  {type}: {count}");
            Console.WriteLine($"Grupos de historial consolidados: {HistoryGroupsConsolidated}");
            Console.WriteLine($"Entradas de PolicyHistory generadas: {HistoryEntriesCreated}");
            Console.WriteLine();
            Console.WriteLine($"Customers creados: {CustomersCreated}");
            Console.WriteLine($"Customers matcheados por SSN: {CustomersMatchedBySsn}");
            Console.WriteLine($"Customers matcheados por Nombre+Apellido+FechaNacimiento: {CustomersMatchedByNameAndDob}");
            Console.WriteLine();
            Console.WriteLine($"Aseguradoras nuevas creadas ({InsuranceCompaniesCreated.Count}): {string.Join(", ", InsuranceCompaniesCreated)}");
            Console.WriteLine();
            Console.WriteLine($"Filas con fallback de Agente ({AgentFallbacks.Count}):");
            foreach (var a in AgentFallbacks.Take(20)) Console.WriteLine($"  fila {a.SourceRow} ({a.SourceFile}): agente CSV \"{a.OriginalAgentName}\" no matcheó -> fallback Admin");
            if (AgentFallbacks.Count > 20) Console.WriteLine($"  ... y {AgentFallbacks.Count - 20} más (ver JSON completo)");
            Console.WriteLine();
            Console.WriteLine($"Advertencias de datos faltantes/inferidos ({MissingDataWarnings.Count}):");
            foreach (var w in MissingDataWarnings.Take(20)) Console.WriteLine($"  {w}");
            if (MissingDataWarnings.Count > 20) Console.WriteLine($"  ... y {MissingDataWarnings.Count - 20} más (ver JSON completo)");
            Console.WriteLine();
            Console.WriteLine($"Filas NO procesables ({UnprocessableRows.Count}):");
            foreach (var w in UnprocessableRows) Console.WriteLine($"  {w}");
            Console.WriteLine();
            Console.WriteLine($"Conflictos de RelacionConPrincipal en dependientes ({RelationConflicts.Count}):");
            foreach (var w in RelationConflicts) Console.WriteLine($"  {w}");
            Console.WriteLine();
            Console.WriteLine($"Advertencias de colisión de SSN ({SsnCollisionWarnings.Count}):");
            foreach (var w in SsnCollisionWarnings) Console.WriteLine($"  {w}");
            Console.WriteLine();
            Console.WriteLine($"PolicyNumber duplicado dentro del run, fila salteada ({SkippedDuplicatePolicyNumbers.Count}):");
            foreach (var s in SkippedDuplicatePolicyNumbers)
                Console.WriteLine($"  PolicyNumber={s.PolicyNumber}: esta fila es \"{s.ThisGroupCustomerName}\" (Customer #{s.ThisGroupCustomerId}, refs {string.Join(",", s.SourceReferences)}) vs. Policy ya existente #{s.ExistingPolicyId} de \"{s.ExistingPolicyCustomerName}\" (Customer #{s.ExistingPolicyCustomerId})");
            Console.WriteLine("===========================================");
        }

        public void SaveJson(string path)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });
            File.WriteAllText(path, json);
        }
    }

    public class AgentFallbackEntry
    {
        public string SourceFile { get; set; } = default!;
        public int SourceRow { get; set; }
        public string OriginalAgentName { get; set; } = default!;
    }

    public class PolicyGroupSummary
    {
        public string SourceFile { get; set; } = default!;
        public string CustomerName { get; set; } = default!;
        public string CompanyName { get; set; } = default!;
        public string PolicyNumber { get; set; } = default!;
        public int SourceRowCount { get; set; }
        public List<string> SourceReferences { get; set; } = new();
        public string CurrentStatus { get; set; } = default!;
    }

    public class SkippedDuplicateEntry
    {
        public string PolicyNumber { get; set; } = default!;
        public string ThisGroupCustomerName { get; set; } = default!;
        public int ThisGroupCustomerId { get; set; }
        public List<string> SourceReferences { get; set; } = new();
        public int ExistingPolicyId { get; set; }
        public int ExistingPolicyCustomerId { get; set; }
        public string ExistingPolicyCustomerName { get; set; } = default!;
    }
}
