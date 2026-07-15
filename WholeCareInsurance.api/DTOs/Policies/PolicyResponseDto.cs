namespace WholeCareInsurance.api.DTOs.Policies
{
    public class PolicyResponseDto
    {
        public int Id { get; set; }
        public string PolicyNumber { get; set; } = default!;
        public string Type { get; set; } = default!;
        public int InsuranceCompanyId { get; set; }
        public string InsuranceCompanyName { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Premium { get; set; }
        public string Status { get; set; } = default!;
        public int Period { get; set; }
        public int? NumberOfApplicants { get; set; }
        public int CustomerId { get; set; }

        public string? PlanType { get; set; }
        public string? InsurancePlan { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public decimal? TaxCreditSubsidy { get; set; }
        public decimal? MonthlyPremiumAmount { get; set; }
    }
}
