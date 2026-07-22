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
        // Metal tier de ACA — DISTINTO de Type (Obama Care/Medicare/Auto/Otro), ambos coexisten.
        public string? PlanType { get; set; }
        public string? InsurancePlan { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public decimal? TaxCreditSubsidy { get; set; }
        public decimal? MonthlyPremiumAmount { get; set; }

        // Campos específicos de Medicare (§12.10). MonthlyPremiumAmount de arriba se reusa.
        public bool? HasMedicaid { get; set; }
        public string? MedicaidLevel { get; set; }
        public bool? ReferredToMedicalCorporation { get; set; }
        public string? MedicalCorporation { get; set; }

        // Campos específicos de Life Insurance (§12.6). Todos opcionales: Type (arriba)
        // también cubre Obama Care/Medicare/Auto/Otro, que no los usan.

        // Coverage
        public bool? AdditionalOrAlternatePolicy { get; set; }
        public string? AdditionalOrAlternatePolicyDetail { get; set; }
        public string? UnderwritingRequirements { get; set; }
        public bool? NeedsMedicalRequirements { get; set; }

        // Premium Information
        public string? BillingType { get; set; }
        public string? PremiumFrequency { get; set; }
        public decimal? PlannedPeriodicModalPremium { get; set; }
        public string? SourceOfFunds { get; set; }

        // Existing Insurance - Primary Insured
        public bool? HasExistingLifeInsurance { get; set; }
        public bool? IsReplacingExistingPolicy { get; set; }
        public bool? UsingFundsFromInforcePolicy { get; set; }
        public bool? ProvideComparativeInfoForm { get; set; }

        // Notice and Consent
        public string? PhysicianName { get; set; }
        public string? PhysicianAddress { get; set; }

        // Extras
        public string? AdditionalInformation { get; set; }
        public bool? ConsentSigned { get; set; }

        // Campos específicos de Supplemental Plans (§12.9). Todos opcionales: Type (arriba)
        // también cubre Obama Care/Medicare/Life Insurance/Auto/Otro, que no los usan.
        // EffectiveDate/InsuranceCompanyId/InsurancePlan/MonthlyPremiumAmount de arriba se
        // reusan (§1.5/§1.11), no se duplican.

        // Cobertura anterior
        public bool? HasExistingDentalCoverage { get; set; }
        public bool? EligibleForMedicare { get; set; }
        public bool? IsReplacingDentalCoverage { get; set; }

        // Datos bancarios — SIN cifrado en reposo (decisión explícita, ver PENDIENTE.md §12.9).
        public bool? InsuredPaysThePremium { get; set; }
        public string? BankAccountType { get; set; }
        public string? RoutingNumber { get; set; }
        public string? AccountNumber { get; set; }
        public bool? InsuredIsAccountHolder { get; set; }
        public bool? AuthorizedAutomaticPayment { get; set; }
        public int? AutoPaymentDay { get; set; }

        // HIPAA y Autorización de Mercadeo
        public bool? AuthorizeMarketingInfo { get; set; }
        public string? RepresentativeName { get; set; }
        public string? RepresentativeRelationship { get; set; }

        // ✅ FK
        public int CustomerId { get; set; }

        // ✅ navegación
        public Customer Customer { get; set; } = default!;

    }
}
