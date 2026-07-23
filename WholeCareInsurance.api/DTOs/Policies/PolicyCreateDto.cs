using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Policies
{
    public class PolicyCreateDto
    {
        public string PolicyNumber { get; set; } = default!;

        [Required]
        [AllowedValues("Health Insurance (ACA)", "Medicare", "Life Insurance", "Supplemental Plans", "Auto", "Otro",
            ErrorMessage = "Tipo de póliza inválido.")]
        public string Type { get; set; } = default!;

        // Validado contra InsuranceCompanies en el controller (existencia), no acá:
        // mismo criterio que CustomerId, es una FK, no un enum de texto.
        public int InsuranceCompanyId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Premium { get; set; }

        [Required]
        [AllowedValues("Draft", "Pendiente", "Cancelado", "Por procesar", "En proceso", "Actualizado", "Procesado", "Cambio de agente",
            ErrorMessage = "Status de póliza inválido.")]
        public string Status { get; set; } = "Draft";
        public int CustomerId { get; set; }

        // Se define desde el selector de Período del header (no es un campo editable
        // del formulario) — igual se valida acá porque igual llega en el body del POST/PUT.
        [Range(2000, 2100, ErrorMessage = "Período inválido.")]
        public int Period { get; set; }

        // Carga manual del agente en la sección de Dependientes, opcional.
        [Range(0, int.MaxValue, ErrorMessage = "El número de aplicantes no puede ser negativo.")]
        public int? NumberOfApplicants { get; set; }

        // Campos confirmados por el análisis del archivo real de migración (Health/Obamacare).
        // Todos opcionales: Type (arriba) también cubre Auto/Otro, que no tienen metal tier
        // ni Tax Credit/Subsidy.

        // Metal tier de ACA — DISTINTO de Type (Obama Care/Medicare/Auto/Otro), ambos coexisten.
        // Sin [AllowedValues] por ser opcional (mismo criterio que Gender/ContactLanguage en Customer).
        [MaxLength(20)] public string? PlanType { get; set; }

        [MaxLength(200)] public string? InsurancePlan { get; set; }

        public DateTime? EffectiveDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El crédito fiscal/subsidio no puede ser negativo.")]
        public decimal? TaxCreditSubsidy { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto de la prima mensual no puede ser negativo.")]
        public decimal? MonthlyPremiumAmount { get; set; }

        // Campos específicos de Medicare (§12.10). Todos opcionales: Type (arriba)
        // también cubre Obama Care/Auto/Otro, que no los usan.
        public bool? HasMedicaid { get; set; }
        [MaxLength(100)] public string? MedicaidLevel { get; set; }
        public bool? ReferredToMedicalCorporation { get; set; }
        [MaxLength(200)] public string? MedicalCorporation { get; set; }

        // Campos específicos de Life Insurance (§12.6). Todos opcionales: Type (arriba)
        // también cubre Obama Care/Medicare/Auto/Otro, que no los usan.

        // Coverage
        public bool? AdditionalOrAlternatePolicy { get; set; }
        [MaxLength(300)] public string? AdditionalOrAlternatePolicyDetail { get; set; }
        [MaxLength(500)] public string? UnderwritingRequirements { get; set; }
        public bool? NeedsMedicalRequirements { get; set; }

        // Premium Information
        [MaxLength(50)] public string? BillingType { get; set; }
        [MaxLength(50)] public string? PremiumFrequency { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "La prima planificada no puede ser negativa.")]
        public decimal? PlannedPeriodicModalPremium { get; set; }

        [MaxLength(200)] public string? SourceOfFunds { get; set; }

        // Existing Insurance - Primary Insured
        public bool? HasExistingLifeInsurance { get; set; }
        public bool? IsReplacingExistingPolicy { get; set; }
        public bool? UsingFundsFromInforcePolicy { get; set; }
        public bool? ProvideComparativeInfoForm { get; set; }

        // Notice and Consent
        [MaxLength(200)] public string? PhysicianName { get; set; }
        [MaxLength(300)] public string? PhysicianAddress { get; set; }

        // Extras
        [MaxLength(2000)] public string? AdditionalInformation { get; set; }
        public bool? ConsentSigned { get; set; }

        // Campos específicos de Supplemental Plans (§12.9). Todos opcionales: Type (arriba)
        // también cubre Obama Care/Medicare/Life Insurance/Auto/Otro, que no los usan.

        // Cobertura anterior
        public bool? HasExistingDentalCoverage { get; set; }
        public bool? EligibleForMedicare { get; set; }
        public bool? IsReplacingDentalCoverage { get; set; }

        // Datos bancarios — SIN cifrado en reposo (decisión explícita, ver PENDIENTE.md §12.9).
        public bool? InsuredPaysThePremium { get; set; }
        [MaxLength(20)] public string? BankAccountType { get; set; }
        [MaxLength(20)] public string? RoutingNumber { get; set; }
        [MaxLength(20)] public string? AccountNumber { get; set; }
        public bool? InsuredIsAccountHolder { get; set; }
        public bool? AuthorizedAutomaticPayment { get; set; }

        [Range(1, 28, ErrorMessage = "El día de pago automático debe estar entre 1 y 28.")]
        public int? AutoPaymentDay { get; set; }

        // HIPAA y Autorización de Mercadeo
        public bool? AuthorizeMarketingInfo { get; set; }
        [MaxLength(200)] public string? RepresentativeName { get; set; }
        [MaxLength(100)] public string? RepresentativeRelationship { get; set; }
    }
}
