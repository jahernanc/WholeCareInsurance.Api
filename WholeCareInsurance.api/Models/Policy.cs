namespace WholeCareInsurance.api.Models
{
    public class Policy
    {

        public int Id { get; set; }

        public string PolicyNumber { get; set; } = default!;

        public string Type { get; set; } = default!;

        public int InsuranceCompanyId { get; set; }
        public InsuranceCompany InsuranceCompany { get; set; } = default!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal Premium { get; set; }

        public string Status { get; set; } = default!;

        public int Period { get; set; }

        public int? NumberOfApplicants { get; set; }

        // Campos confirmados por el análisis del archivo real de migración (Health/Obamacare).
        // Metal tier de ACA — DISTINTO de Type (Obama Care/Salud/Auto/Otro), ambos coexisten.
        public string? PlanType { get; set; }
        public string? InsurancePlan { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public decimal? TaxCreditSubsidy { get; set; }
        public decimal? MonthlyPremiumAmount { get; set; }

        // ✅ FK
        public int CustomerId { get; set; }

        // ✅ navegación
        public Customer Customer { get; set; } = default!;

    }
}
