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

        [Required]
        [AllowedValues("WholeCareInsurance", "Otro",
            ErrorMessage = "Compañía aseguradora inválida.")]
        public string InsuranceCompany { get; set; } = default!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Premium { get; set; }

        [Required]
        [AllowedValues("Draft", "Pendiente", "Cancelado", "Por procesar", "En proceso", "En corrección", "Procesado", "Cambio de agente",
            ErrorMessage = "Status de póliza inválido.")]
        public string Status { get; set; } = "Draft";
        public int CustomerId { get; set; }
    }
}
