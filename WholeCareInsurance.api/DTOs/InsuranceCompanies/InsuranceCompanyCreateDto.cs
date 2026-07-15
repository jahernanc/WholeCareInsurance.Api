using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.InsuranceCompanies
{
    public class InsuranceCompanyCreateDto
    {
        [Required][MaxLength(150)] public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}
