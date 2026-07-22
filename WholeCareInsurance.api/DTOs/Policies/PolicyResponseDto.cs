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

        public bool? HasMedicaid { get; set; }
        public string? MedicaidLevel { get; set; }
        public bool? ReferredToMedicalCorporation { get; set; }
        public string? MedicalCorporation { get; set; }

        public bool? AdditionalOrAlternatePolicy { get; set; }
        public string? AdditionalOrAlternatePolicyDetail { get; set; }
        public string? UnderwritingRequirements { get; set; }
        public bool? NeedsMedicalRequirements { get; set; }

        public string? BillingType { get; set; }
        public string? PremiumFrequency { get; set; }
        public decimal? PlannedPeriodicModalPremium { get; set; }
        public string? SourceOfFunds { get; set; }

        public bool? HasExistingLifeInsurance { get; set; }
        public bool? IsReplacingExistingPolicy { get; set; }
        public bool? UsingFundsFromInforcePolicy { get; set; }
        public bool? ProvideComparativeInfoForm { get; set; }

        public string? PhysicianName { get; set; }
        public string? PhysicianAddress { get; set; }

        public string? AdditionalInformation { get; set; }
        public bool? ConsentSigned { get; set; }

        public bool? HasExistingDentalCoverage { get; set; }
        public bool? EligibleForMedicare { get; set; }
        public bool? IsReplacingDentalCoverage { get; set; }

        public bool? InsuredPaysThePremium { get; set; }
        public string? BankAccountType { get; set; }
        public string? RoutingNumber { get; set; }
        public string? AccountNumber { get; set; }
        public bool? InsuredIsAccountHolder { get; set; }
        public bool? AuthorizedAutomaticPayment { get; set; }
        public int? AutoPaymentDay { get; set; }

        public bool? AuthorizeMarketingInfo { get; set; }
        public string? RepresentativeName { get; set; }
        public string? RepresentativeRelationship { get; set; }
    }
}
