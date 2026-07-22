using System.ComponentModel.DataAnnotations;

namespace WholeCareInsurance.api.DTOs.Policies
{
    public class BeneficiaryCreateDto
    {
        [Required][MaxLength(50)] public string TypeOfRelationship { get; set; } = default!;
        [Required][MaxLength(100)] public string FirstName { get; set; } = default!;
        [Required][MaxLength(100)] public string LastName { get; set; } = default!;
        [Required] public DateTime DateOfBirth { get; set; }

        [MaxLength(20)] public string? Gender { get; set; }
        [MaxLength(20)] public string? Phone { get; set; }
        [EmailAddress][MaxLength(200)] public string? Email { get; set; }
        [MaxLength(20)] public string? SocialSecurityNumber { get; set; }
    }
}
