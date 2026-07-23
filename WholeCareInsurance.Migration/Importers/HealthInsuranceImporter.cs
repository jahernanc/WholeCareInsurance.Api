using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    public static class HealthInsuranceImporter
    {
        public const string SourceFileLabel = "Health/Obamacare";
        public const string Type = "Health Insurance (ACA)";

        public static async Task<List<PreparedPolicyGroup>> RunAsync(string filePath, ImportPipeline pipeline)
        {
            var rows = ExcelReader.ReadRows(filePath);

            return await pipeline.PrepareAsync(
                SourceFileLabel,
                rows,
                Type,
                populateTypeSpecificFields: (policy, row, fields) =>
                {
                    policy.PlanType = row.GetString("Type of plan");
                    policy.InsurancePlan = row.GetString("Insurance plan");
                    policy.TaxCreditSubsidy = row.GetDecimal("Tax Credit / Subsidy");
                    policy.MonthlyPremiumAmount = row.GetDecimal("Monthly premium amount");

                    var consent = row.GetString("Confirmed consent");
                    if (consent != null)
                        policy.ConsentSigned = string.Equals(consent, "Yes", StringComparison.OrdinalIgnoreCase);
                },
                extractDependents: HealthDependentsExtractor.ExtractAsync);
        }
    }
}
