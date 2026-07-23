using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.Migration.Consolidation;
using WholeCareInsurance.Migration.Excel;
using WholeCareInsurance.Migration.Lookups;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    // Una fila ya resuelta (Customer/Company matcheados o creados) lista para agrupar.
    public class ResolvedRow
    {
        public required ExcelRow Row { get; init; }
        public required RawPolicyFields Fields { get; init; }
        public required int CustomerId { get; init; }
        public required int InsuranceCompanyId { get; init; }
    }

    // Preparado para persistir: una Policy consolidada + su historial + dependientes,
    // listo para que Program.cs lo envuelva en su propia transacción/savepoint.
    public class PreparedPolicyGroup
    {
        public required Policy Policy { get; init; }
        public required List<HistoryTransition> HistoryTransitions { get; init; }
        public required List<DependentLink> Dependents { get; init; }
        public required int SourceRowCount { get; init; }
        public required List<string> SourceReferences { get; init; }
        public required string SourceFile { get; init; }
        public required string CustomerName { get; init; }
        public required string CompanyName { get; init; }
        public required int TitularCustomerId { get; init; }
        public required int? CurrentRowAgentUserId { get; init; }
    }

    // Pipeline compartido por los 4 importers: parseo de columnas comunes, resolución
    // de Customer/InsuranceCompany/Agente fila por fila, agrupamiento por historial y
    // armado de la Policy + PolicyHistory + PolicyDependents consolidados. Cada importer
    // de tipo (Health/Medicare/Life/Supplemental) aporta el delegate que llena los campos
    // específicos de su Type y, si corresponde, extrae dependientes.
    public class ImportPipeline
    {
        private readonly AppDbContext _db;
        private readonly EntityMatcher _matcher;
        private readonly MigrationReport _report;
        private readonly int _historyWindowDays;

        public ImportPipeline(AppDbContext db, EntityMatcher matcher, MigrationReport report, int historyWindowDays)
        {
            _db = db;
            _matcher = matcher;
            _report = report;
            _historyWindowDays = historyWindowDays;
        }

        public async Task<List<PreparedPolicyGroup>> PrepareAsync(
            string sourceFile,
            List<ExcelRow> rows,
            string policyType,
            Action<Policy, ExcelRow, RawPolicyFields> populateTypeSpecificFields,
            Func<ExcelRow, MigrationReport, string, EntityMatcher, Task<List<DependentLink>>>? extractDependents = null)
        {
            var knownPlanNames = rows
                .Select(r => r.GetString("Insurance plan"))
                .Where(v => v != null)
                .Select(v => v!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var resolved = new List<ResolvedRow>();

            foreach (var row in rows)
            {
                var fields = CommonFieldsExtractor.ExtractPolicyFields(row, _report, sourceFile);

                // Se resuelve el Agente de TODAS las filas (no solo la vigente) para que
                // el reporte liste cada fallback, tal como se pidió.
                await _matcher.ResolveAgentAsync(sourceFile, row.RowNumber, fields.AgentNameRaw);

                var customerData = CommonFieldsExtractor.ExtractCustomer(row, fields.Reference);
                customerData.RelacionConPrincipal = EnumMaps.TitularRelacionConPrincipal;
                var customerMatch = await _matcher.ResolveCustomerAsync(customerData);
                RecordCustomerMatchMetric(customerMatch.Kind);

                var companyId = await _matcher.ResolveInsuranceCompanyAsync(fields.CompanyNameRaw);

                resolved.Add(new ResolvedRow
                {
                    Row = row,
                    Fields = fields,
                    CustomerId = customerMatch.CustomerId,
                    InsuranceCompanyId = companyId,
                });
            }

            // Un "Policy number" real pertenece a una sola persona+aseguradora. Si el
            // mismo valor crudo aparece asociado a más de una combinación (Customer,
            // InsuranceCompany) distinta en el archivo, es un código genérico (confirmado:
            // formato tipo HIOS compartido por hasta 10 apellidos sin relación entre sí)
            // y no sirve como identificador único — se trata como si faltara.
            var policyNumberToGroups = new Dictionary<string, HashSet<(int CustomerId, int CompanyId)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in resolved)
            {
                var pn = r.Fields.PolicyNumberRaw?.Trim();
                if (string.IsNullOrEmpty(pn)) continue;
                if (!policyNumberToGroups.TryGetValue(pn, out var set))
                    policyNumberToGroups[pn] = set = new HashSet<(int, int)>();
                set.Add((r.CustomerId, r.InsuranceCompanyId));
            }
            var sharedAcrossDifferentPeople = policyNumberToGroups
                .Where(kv => kv.Value.Count > 1)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var pn in sharedAcrossDifferentPeople)
                _report.MissingDataWarnings.Add(
                    $"{sourceFile}: \"Policy number\"=\"{pn}\" está asociado a {policyNumberToGroups[pn].Count} combinaciones distintas de Customer+Aseguradora (código genérico de plan, no un número de póliza individual) — se usa Reference como fallback para cada una.");

            var groups = PolicyGrouper.Group(
                resolved.Select(r => new ConsolidationRow<ResolvedRow>
                {
                    Source = r,
                    CustomerId = r.CustomerId,
                    InsuranceCompanyId = r.InsuranceCompanyId,
                    EffectiveDate = r.Fields.EffectiveDate,
                    UpdateDate = r.Fields.UpdateDate,
                    RegistrationDate = r.Fields.RegistrationDate,
                    Status = r.Fields.StatusRaw,
                }),
                _historyWindowDays);

            var prepared = new List<PreparedPolicyGroup>();

            foreach (var chain in groups)
            {
                var sortedByEffective = chain.OrderBy(c => c.EffectiveDate).ToList();
                var current = PolicyGrouper.CurrentOf(chain);
                var currentRow = current.Source;

                var status = EnumMaps.PolicyStatus.TryGetValue(currentRow.Fields.StatusRaw, out var mappedStatus)
                    ? mappedStatus
                    : LogUnmappedStatus(sourceFile, currentRow);

                var policyNumber = PolicyNumberResolver.Resolve(
                    currentRow.Fields.PolicyNumberRaw, currentRow.Fields.Reference, knownPlanNames, sharedAcrossDifferentPeople);

                // El origen no tiene fecha de fin explícita (aprobado): se infiere el
                // 31/12 del Period de la fila vigente.
                var endDate = new DateTime(currentRow.Fields.Period, 12, 31);
                _report.MissingDataWarnings.Add(
                    $"{sourceFile} PolicyNumber={policyNumber}: EndDate inferido como 31/12/{currentRow.Fields.Period} (el origen no tiene fecha de fin explícita).");

                var policy = new Policy
                {
                    PolicyNumber = policyNumber,
                    Type = policyType,
                    InsuranceCompanyId = currentRow.InsuranceCompanyId,
                    StartDate = sortedByEffective.First().EffectiveDate,
                    EndDate = endDate,
                    Premium = 0,
                    Status = status,
                    Period = currentRow.Fields.Period,
                    NumberOfApplicants = currentRow.Fields.NumberOfApplicants,
                    CustomerId = currentRow.CustomerId,
                    // Común a los 4 archivos ("Effective date"); InsurancePlan/PlanType/
                    // montos quedan a cargo del delegate type-specific de cada importer.
                    EffectiveDate = currentRow.Fields.EffectiveDate,
                };

                populateTypeSpecificFields(policy, currentRow.Row, currentRow.Fields);

                // Premium (obligatorio en todos los Types) reusa MonthlyPremiumAmount
                // cuando el origen lo trae (solo Health) — es la única cifra de prima
                // disponible en el CSV, aprobado como default razonable.
                if (policy.MonthlyPremiumAmount is > 0)
                    policy.Premium = policy.MonthlyPremiumAmount.Value;
                else
                    _report.MissingDataWarnings.Add($"{sourceFile} PolicyNumber={policyNumber}: sin monto de prima en el origen, Premium quedó en 0.");

                var transitions = new List<HistoryTransition>
                {
                    new() { OldStatus = null, NewStatus = MapStatusOrRaw(sortedByEffective[0].Status), ChangedAt = sortedByEffective[0].RegistrationDate },
                };
                for (int i = 1; i < sortedByEffective.Count; i++)
                {
                    transitions.Add(new HistoryTransition
                    {
                        OldStatus = MapStatusOrRaw(sortedByEffective[i - 1].Status),
                        NewStatus = MapStatusOrRaw(sortedByEffective[i].Status),
                        ChangedAt = sortedByEffective[i].UpdateDate,
                    });
                }

                List<DependentLink> dependents = new();
                if (extractDependents != null)
                    dependents = await extractDependents(currentRow.Row, _report, sourceFile, _matcher);

                // Un bloque de dependiente puede matchear (por SSN o Nombre+Apellido+
                // FechaNacimiento) al MISMO Customer que ya es el titular de la Policy
                // (CustomerId) — no tiene sentido que sea su propio dependiente.
                var selfDependent = dependents.FirstOrDefault(d => d.CustomerId == currentRow.CustomerId);
                if (selfDependent != null)
                {
                    dependents = dependents.Where(d => d.CustomerId != currentRow.CustomerId).ToList();
                    _report.MissingDataWarnings.Add(
                        $"{sourceFile} PolicyNumber={policyNumber}: un bloque de dependiente matcheó al mismo Customer que el titular (#{currentRow.CustomerId}), se descartó ese vínculo.");
                }

                int? agentUserId = null;
                if (!string.IsNullOrWhiteSpace(currentRow.Fields.AgentNameRaw))
                    agentUserId = await _matcher.ResolveAgentAsync(sourceFile, currentRow.Row.RowNumber, currentRow.Fields.AgentNameRaw);

                prepared.Add(new PreparedPolicyGroup
                {
                    Policy = policy,
                    HistoryTransitions = transitions,
                    Dependents = dependents,
                    SourceRowCount = chain.Count,
                    SourceReferences = chain.Select(c => c.Source.Fields.Reference).ToList(),
                    SourceFile = sourceFile,
                    CustomerName = $"{currentRow.Row.GetString("First name")} {currentRow.Row.GetString("Last name")}".Trim(),
                    CompanyName = currentRow.Fields.CompanyNameRaw,
                    TitularCustomerId = currentRow.CustomerId,
                    CurrentRowAgentUserId = agentUserId,
                });
            }

            return prepared;
        }

        private string MapStatusOrRaw(string rawStatus)
            => EnumMaps.PolicyStatus.TryGetValue(rawStatus, out var mapped) ? mapped : rawStatus;

        private string LogUnmappedStatus(string sourceFile, ResolvedRow row)
        {
            _report.MissingDataWarnings.Add(
                $"{sourceFile} fila {row.Row.RowNumber} (Ref {row.Fields.Reference}): Status \"{row.Fields.StatusRaw}\" no tiene mapeo conocido, se usa tal cual.");
            return row.Fields.StatusRaw;
        }

        private void RecordCustomerMatchMetric(CustomerMatchKind kind)
        {
            switch (kind)
            {
                case CustomerMatchKind.Created: _report.CustomersCreated++; break;
                case CustomerMatchKind.MatchedBySsn: _report.CustomersMatchedBySsn++; break;
                case CustomerMatchKind.MatchedByNameDob: _report.CustomersMatchedByNameAndDob++; break;
            }
        }

        // Backfill de Customer.AgentId (solo si está vacío) usando el Agente de la fila
        // vigente del grupo — se llama justo antes de persistir cada Policy, dentro de
        // la misma transacción/savepoint de esa unidad.
        public async Task BackfillAgentIfMissingAsync(int customerId, int? agentUserId)
        {
            if (agentUserId is null) return;
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer != null && customer.AgentId is null)
                customer.AgentId = agentUserId;
        }
    }
}
