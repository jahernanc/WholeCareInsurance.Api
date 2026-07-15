using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Policies
{
    public class PolicyCreateDto
    {
        public string PolicyNumber { get; set; } = default!;

        [Required]
        [AllowedValues("Obama Care", "Salud", "Auto", "Otro",
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

        // Metal tier de ACA — DISTINTO de Type (Obama Care/Salud/Auto/Otro), ambos coexisten.
        // Sin [AllowedValues] por ser opcional (mismo criterio que Gender/ContactLanguage en Customer).
        [MaxLength(20)] public string? PlanType { get; set; }

        [MaxLength(200)] public string? InsurancePlan { get; set; }

        public DateTime? EffectiveDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El crédito fiscal/subsidio no puede ser negativo.")]
        public decimal? TaxCreditSubsidy { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto de la prima mensual no puede ser negativo.")]
        public decimal? MonthlyPremiumAmount { get; set; }
    }
}
