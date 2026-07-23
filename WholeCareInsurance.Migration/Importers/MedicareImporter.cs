using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    public static class MedicareImporter
    {
        public const string SourceFileLabel = "Medicare";
        public const string Type = "Medicare";

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
                    policy.PlanType = row.GetString("Type of plan");
                    // HasMedicaid/MedicaidLevel/ReferredToMedicalCorporation/MedicalCorporation:
                    // no están en este CSV (confirmado), quedan null/default.
                });
        }
    }
}
