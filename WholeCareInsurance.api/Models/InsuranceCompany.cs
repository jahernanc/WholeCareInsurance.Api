namespace WholeCareInsurance.api.Models
{
    public class InsuranceCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}
