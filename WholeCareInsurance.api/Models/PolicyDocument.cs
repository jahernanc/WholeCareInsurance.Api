namespace WholeCareInsurance.api.Models
{
    public class PolicyDocument
    {
        public int Id { get; set; }

        public int PolicyId { get; set; }
        public Policy Policy { get; set; } = default!;

        public string OriginalFileName { get; set; } = default!;
        public string StoredFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
