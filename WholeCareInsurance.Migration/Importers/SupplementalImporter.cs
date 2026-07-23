using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    public static class SupplementalImporter
    {
        public const string SourceFileLabel = "Supplemental Plans";
        public const string Type = "Supplemental Plans";

        public static async Task<List<PreparedPolicyGroup>> RunAsync(string filePath, ImportPipeline pipeline)
        {
            var rows = ExcelReader.ReadRows(filePath);

            return await pipeline.PrepareAsync(
                SourceFileLabel,
                rows,
                Type,
                populateTypeSpecificFields: (policy, row, fields) =>
                {
                    policy.InsurancePlan = row.GetString("Insurance plan");
                    // Cobertura anterior / datos bancarios / HIPAA: no están en este CSV
                    // (confirmado), quedan null/default.
                });
        }
    }
}
