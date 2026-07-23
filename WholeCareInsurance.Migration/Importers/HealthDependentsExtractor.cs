using WholeCareInsurance.Migration.Excel;
using WholeCareInsurance.Migration.Lookups;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    // Extrae los hasta 8 dependientes de la fila VIGENTE del grupo (columnas
    // "First name_1".."First name_8" etc.) y los matchea/crea como Customer,
    // vinculándolos vía PolicyDependents. Solo aplica a Health/Obamacare.
    public static class HealthDependentsExtractor
    {
        public static async Task<List<DependentLink>> ExtractAsync(
            ExcelRow row, MigrationReport report, string sourceFile, EntityMatcher matcher)
        {
            var links = new List<DependentLink>();

            for (int i = 1; i <= 8; i++)
            {
                var suffix = $"_{i}";
                var firstName = row.GetString($"First name{suffix}");
                if (firstName == null) continue;

                var reference = row.GetString("Reference") ?? $"NOREF-{sourceFile}-{row.RowNumber}";
                var customerData = CommonFieldsExtractor.ExtractCustomer(row, reference, suffix);

                var depTypeRaw = row.GetString($"Dependency type{suffix}");
                customerData.RelacionConPrincipal = depTypeRaw != null && EnumMaps.DependencyType.TryGetValue(depTypeRaw, out var rel)
                    ? rel
                    : "Otro";
                if (depTypeRaw != null && !EnumMaps.DependencyType.ContainsKey(depTypeRaw))
                    report.MissingDataWarnings.Add(
                        $"{sourceFile} fila {row.RowNumber} dependiente {i}: \"Dependency type\" = \"{depTypeRaw}\" sin mapeo conocido, se usó \"Otro\".");

                var match = await matcher.ResolveCustomerAsync(customerData);
                switch (match.Kind)
                {
                    case CustomerMatchKind.Created: report.CustomersCreated++; break;
                    case CustomerMatchKind.MatchedBySsn: report.CustomersMatchedBySsn++; break;
                    case CustomerMatchKind.MatchedByNameDob: report.CustomersMatchedByNameAndDob++; break;
                }
                if (match.Kind != CustomerMatchKind.Created)
                {
                    // El Customer ya existía con su propio RelacionConPrincipal (es un
                    // campo del Customer, no de la relación PolicyDependent) — no se
                    // pisa; se deja constancia si el valor difiere del que trae esta fila.
                    report.RelationConflicts.Add(
                        $"{sourceFile} fila {row.RowNumber} dependiente {i} (\"{customerData.FirstName} {customerData.LastName}\"): " +
                        $"ya existía como Customer #{match.CustomerId}, se mantiene su RelacionConPrincipal actual en vez de \"{customerData.RelacionConPrincipal}\".");
                }

                var isApplicantFlag = row.GetString($"Is this member an applicant?{suffix}");
                var isAplicante = !string.Equals(isApplicantFlag, "No", StringComparison.OrdinalIgnoreCase);

                links.Add(new DependentLink { CustomerId = match.CustomerId, IsAplicante = isAplicante });
            }

            // Dos bloques de dependiente pueden matchear al MISMO Customer (SSN o
            // Nombre+Apellido+FechaNacimiento repetidos entre slots) — PolicyDependents
            // tiene clave {PolicyId, CustomerId}, así que un duplicado rompe el INSERT.
            // Se deduplica quedándose con la primera aparición (IsAplicante = true si
            // CUALQUIERA de las apariciones lo marcaba así).
            var deduped = links
                .GroupBy(l => l.CustomerId)
                .Select(g => new DependentLink { CustomerId = g.Key, IsAplicante = g.Any(l => l.IsAplicante) })
                .ToList();
            if (deduped.Count != links.Count)
                report.MissingDataWarnings.Add(
                    $"{sourceFile} fila {row.RowNumber}: {links.Count - deduped.Count} bloque(s) de dependiente matchearon a un Customer ya listado en la misma fila, se deduplicó.");

            return deduped;
        }
    }
}
