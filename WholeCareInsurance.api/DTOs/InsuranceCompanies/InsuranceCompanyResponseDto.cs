namespace WholeCareInsurance.api.DTOs.InsuranceCompanies
{
    public class InsuranceCompanyResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
