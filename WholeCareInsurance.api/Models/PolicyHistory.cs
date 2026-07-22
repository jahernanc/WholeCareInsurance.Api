namespace WholeCareInsurance.api.Models
{
    public class PolicyHistory
    {
        public int Id { get; set; }

        public int PolicyId { get; set; }
        public Policy Policy { get; set; } = default!;

        public string FieldChanged { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }

        // Nullable a propósito: la carga histórica desde el futuro script de
        // migración no tiene un usuario real asociado a cada snapshot reconstruido.
        public int? ChangedByUserId { get; set; }
        public User? ChangedByUser { get; set; }

        // "Sistema" (tracking en vivo) | "Migración" (carga histórica bulk).
        public string Source { get; set; } = "Sistema";
    }
}
