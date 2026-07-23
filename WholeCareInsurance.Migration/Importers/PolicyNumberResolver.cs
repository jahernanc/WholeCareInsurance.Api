namespace WholeCareInsurance.Migration.Importers
{
    // "Policy number" en el CSV real tiene mala calidad: de 1258 filas de Health solo 70
    // valores no vacíos, y algunos son basura evidente. Dos formas de basura confirmadas
    // contra datos reales:
    // 1) Nombre de plan tipeado por error en la celda (ej. "Silver Simple PCP Saver CSR
    //    150") — se detecta comparando contra el set de "Insurance plan" del archivo.
    // 2) Código genérico de plan ACA (formato HIOS, ej. "49004FL0010002-01") cargado en
    //    vez del número real de póliza — reaparece en filas de personas SIN relación
    //    entre sí (confirmado: un mismo valor compartido por hasta 10 apellidos distintos
    //    con la misma aseguradora). Se detecta porque el mismo valor crudo queda asociado
    //    a más de una combinación (Customer, InsuranceCompany) distinta en el archivo —
    //    un número de póliza real pertenece a una sola persona+aseguradora.
    // En ambos casos se usa el "Reference" de la fila (único por fila en el origen) como
    // PolicyNumber de fallback, para que cada persona reciba su propia Policy.
    public static class PolicyNumberResolver
    {
        public static string Resolve(
            string? rawPolicyNumber,
            string reference,
            HashSet<string> knownPlanNames,
            HashSet<string> sharedAcrossDifferentPeople)
        {
            if (string.IsNullOrWhiteSpace(rawPolicyNumber)) return reference;
            var trimmed = rawPolicyNumber.Trim();
            if (knownPlanNames.Contains(trimmed)) return reference;
            if (sharedAcrossDifferentPeople.Contains(trimmed)) return reference;
            return trimmed;
        }
    }
}
