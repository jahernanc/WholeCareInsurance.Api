using WholeCareInsurance.Migration.Excel;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    // Campos comunes a los 4 archivos (identidad, dirección, póliza básica).
    public class RawPolicyFields
    {
        public required string Reference { get; init; }
        public string? PolicyNumberRaw { get; init; }
        public required string AgentNameRaw { get; init; }
        public required string CompanyNameRaw { get; init; }
        public required string StatusRaw { get; init; }
        public required int Period { get; init; }
        public required DateTime EffectiveDate { get; init; }
        public required DateTime RegistrationDate { get; init; }
        public required DateTime UpdateDate { get; init; }
        public int? NumberOfApplicants { get; init; }
    }

    public static class CommonFieldsExtractor
    {
        public static CustomerSourceData ExtractCustomer(ExcelRow row, string reference, string suffix = "")
        {
            return new CustomerSourceData
            {
                SourceReference = reference + suffix,
                SocialSecurityNumber = row.GetString($"Social Security Number{suffix}"),
                FirstName = row.GetString($"First name{suffix}") ?? "",
                LastName = row.GetString($"Last name{suffix}") ?? "",
                DateOfBirth = row.GetDate($"Date of birth{suffix}") ?? new DateTime(1900, 1, 1),
                Email = row.GetString($"Email{suffix}"),
                Address1 = suffix == "" ? row.GetString("Address # 1") : null,
                Phone = row.GetString($"Phone{suffix}"),
                LegalStatus = row.GetString($"Legal Status{suffix}"),
                ZipCode = suffix == "" ? row.GetString("Zip code") : null,
                State = suffix == "" ? row.GetString("State / Province") : null,
                City = suffix == "" ? row.GetString("City") : null,
                County = suffix == "" ? row.GetString("County") : null,
                MaritalStatus = suffix == "" ? row.GetString("Estado civil") : null,
                Occupation = suffix == "" ? row.GetString("Position / Occupation") : null,
                MiddleName = row.GetString($"Middle name{suffix}"),
                Gender = row.GetString($"Gender{suffix}"),
                GreenCard = suffix == "" ? row.GetString("Green card") : null,
                WorkPermit = suffix == "" ? row.GetString("Work permit") : null,
                Address2 = suffix == "" ? row.GetString("Address # 2") : null,
                EmployerName = suffix == "" ? row.GetString("Employer name") : null,
                CompanyPhone = suffix == "" ? row.GetString("Company Phone") : null,
                AnnualIncome = suffix == "" ? row.GetDecimal("Annual income") : null,
            };
        }

        public static RawPolicyFields ExtractPolicyFields(ExcelRow row, MigrationReport report, string sourceFile)
        {
            var reference = row.GetString("Reference") ?? $"NOREF-{sourceFile}-{row.RowNumber}";
            var effectiveDate = row.GetDate("Effective date");
            var registrationDate = row.GetDateTime("Registration date");
            var updateDate = row.GetDateTime("Update date");

            if (effectiveDate == null)
            {
                report.MissingDataWarnings.Add($"{sourceFile} fila {row.RowNumber} (Ref {reference}): sin Effective date, se usa Registration date.");
            }
            if (registrationDate == null || updateDate == null)
            {
                report.MissingDataWarnings.Add($"{sourceFile} fila {row.RowNumber} (Ref {reference}): sin Registration/Update date, se usan valores por defecto para consolidar historial.");
            }

            var effective = effectiveDate ?? registrationDate ?? DateTime.UtcNow;
            var registration = registrationDate ?? effective;
            var update = updateDate ?? registration;

            return new RawPolicyFields
            {
                Reference = reference,
                PolicyNumberRaw = row.GetString("Policy number"),
                AgentNameRaw = row.GetString("Agent") ?? "",
                CompanyNameRaw = row.GetString("Company") ?? "Desconocida",
                StatusRaw = row.GetString("Status") ?? "Draft",
                Period = row.GetInt("Period") ?? effective.Year,
                EffectiveDate = effective,
                RegistrationDate = registration,
                UpdateDate = update,
                NumberOfApplicants = row.GetInt("Number of applicants"),
            };
        }
    }
}
