namespace WholeCareInsurance.Migration.Consolidation
{
    // Una fila de origen ya resuelta (Customer/Company matcheados) lista para agrupar.
    public class ConsolidationRow<TSource>
    {
        public required TSource Source { get; init; }
        public required int CustomerId { get; init; }
        public required int InsuranceCompanyId { get; init; }
        public required DateTime EffectiveDate { get; init; }
        public required DateTime UpdateDate { get; init; }
        public required DateTime RegistrationDate { get; init; }
        public required string Status { get; init; }
    }

    // Agrupa filas de UN MISMO archivo/Type por (CustomerId, InsuranceCompanyId) y las
    // encadena por cercanía de EffectiveDate (ventana configurable, transitiva: A-B y
    // B-C dentro de la ventana unen A-B-C aunque A-C la exceda). Ver justificación en el
    // diseño: gaps reales observados en Health van de 0 a 184 días, en escalones de ~30
    // (ciclos mensuales de renovación ACA) — de ahí el default de 200 días.
    public static class PolicyGrouper
    {
        public static List<List<ConsolidationRow<TSource>>> Group<TSource>(
            IEnumerable<ConsolidationRow<TSource>> rows, int historyWindowDays)
        {
            var byKey = rows.GroupBy(r => (r.CustomerId, r.InsuranceCompanyId));
            var result = new List<List<ConsolidationRow<TSource>>>();

            foreach (var group in byKey)
            {
                var sorted = group.OrderBy(r => r.EffectiveDate).ToList();
                var currentChain = new List<ConsolidationRow<TSource>> { sorted[0] };

                for (int i = 1; i < sorted.Count; i++)
                {
                    var gapDays = (sorted[i].EffectiveDate - sorted[i - 1].EffectiveDate).TotalDays;
                    if (gapDays <= historyWindowDays)
                    {
                        currentChain.Add(sorted[i]);
                    }
                    else
                    {
                        result.Add(currentChain);
                        currentChain = new List<ConsolidationRow<TSource>> { sorted[i] };
                    }
                }
                result.Add(currentChain);
            }

            return result;
        }

        // La fila con Update date más reciente define el estado actual de la Policy.
        public static ConsolidationRow<TSource> CurrentOf<TSource>(List<ConsolidationRow<TSource>> chain)
            => chain.OrderByDescending(r => r.UpdateDate).First();
    }
}
