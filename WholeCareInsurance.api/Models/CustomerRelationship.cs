namespace WholeCareInsurance.api.Models
{
    public class CustomerRelationship
    {
        public int Id { get; set; }

        public int TitularCustomerId { get; set; }
        public Customer TitularCustomer { get; set; } = default!;

        public int DependentCustomerId { get; set; }
        public Customer DependentCustomer { get; set; } = default!;

        // Copiado de Customer.RelacionConPrincipal al crear el vínculo (§24.1) —
        // por relación, no por persona: el mismo dependiente puede tener un
        // parentesco distinto con cada titular.
        public string? RelationshipType { get; set; }

        // "Sistema" (alta manual) | "Migración" (backfill masivo, §24.2).
        public string Source { get; set; } = "Sistema";

        public DateTime CreatedAt { get; set; }
    }
}
