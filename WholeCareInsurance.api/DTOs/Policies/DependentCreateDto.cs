using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Policies
{
    public class DependentCreateDto
    {
        [Required] public int CustomerId { get; set; }
        public bool IsAplicante { get; set; } = false;
    }
}
