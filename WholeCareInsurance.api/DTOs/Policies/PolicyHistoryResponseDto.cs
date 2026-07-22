namespace WholeCareInsurance.api.DTOs.Policies
{
    public class PolicyHistoryResponseDto
    {
        public int Id { get; set; }
        public string FieldChanged { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public int? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public string Source { get; set; } = default!;
    }
}
