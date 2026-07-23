using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    public static class LifeInsuranceImporter
    {
        public const string SourceFileLabel = "Life Insurance";
        public const string Type = "Life Insurance";

        public static async Task<List<PreparedPolicyGroup>> RunAsync(string filePath, ImportPipeline pipeline)
        {
            var rows = ExcelReader.ReadRows(filePath);

            return await pipeline.PrepareAsync(
                SourceFileLabel,
                rows,
                Type,
                populateTypeSpecificFields: (policy, row, fields) =>
                {
                    // Sin columnas propias en este CSV (confirmado): coverage, premium
                    // info, existing-insurance, notice/consent y beneficiarios quedan
                    // null/default — PolicyBeneficiaries queda vacía.
                });
        }
    }
}
