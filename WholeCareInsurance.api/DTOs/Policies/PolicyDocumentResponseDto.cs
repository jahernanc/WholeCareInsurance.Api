namespace WholeCareInsurance.api.DTOs.Policies
{
    public class PolicyDocumentResponseDto
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
