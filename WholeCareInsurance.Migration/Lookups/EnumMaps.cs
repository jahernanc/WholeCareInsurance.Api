namespace WholeCareInsurance.Migration.Lookups
{
    // Mapeos 1:1 confirmados contra los valores REALES observados en los 4 Excel
    // (ver análisis previo a la implementación). Cualquier valor de origen no
    // contemplado acá devuelve null vía TryGetValue del llamador, y el importer
    // lo reporta como advertencia en vez de asumir un default silencioso.
    public static class EnumMaps
    {
        // Status del reporte viejo -> Policy.Status (biyectivo, 8 <-> 8).
        public static readonly Dictionary<string, string> PolicyStatus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Draft"] = "Draft",
            ["Processed"] = "Procesado",
            ["Updated"] = "Actualizado",
            ["Canceled"] = "Cancelado",
            ["To be processed"] = "Por procesar",
            ["In Process"] = "En proceso",
            ["Agent change"] = "Cambio de agente",
            ["Pending"] = "Pendiente",
        };

        // Legal Status -> Customer.MigrationStatus (biyectivo, 5 <-> 5).
        public static readonly Dictionary<string, string> MigrationStatus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Work Permit"] = "Permiso de trabajo",
            ["Resident"] = "Residente permanente",
            ["Citizen"] = "Ciudadano",
            ["Other"] = "Otro",
            ["Asylum"] = "Asilo",
        };

        // Estado civil (ya en español en el CSV, sin el "/a") -> Customer.MaritalStatus.
        public static readonly Dictionary<string, string> MaritalStatus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Soltero"] = "Soltero/a",
            ["Casado"] = "Casado/a",
            ["Divorciado"] = "Divorciado/a",
            ["Viudo"] = "Viudo/a",
        };

        public static readonly Dictionary<string, string> Gender = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Male"] = "Masculino",
            ["Female"] = "Femenino",
        };

        // Dependency type -> Customer.RelacionConPrincipal. Spouse/Child del pedido
        // original + Stepchild/Grandchildren/Brother (equivalentes exactos que
        // encontramos en el enum real, no estaban en el ejemplo pero mejoran fidelidad
        // vs. mandar todo a "Otro"). Parent/Dependent -> Otro, tal como se acordó.
        public static readonly Dictionary<string, string> DependencyType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Spouse"] = "Cónyuge",
            ["Child"] = "Hijo/a",
            ["Stepchild"] = "Hijastro/a",
            ["Grandchildren"] = "Nieto/a",
            ["Brother"] = "Hermano/a",
            ["Parent"] = "Otro",
            ["Dependent"] = "Otro",
        };

        // Valor por defecto para el titular/principal de la póliza: el enum de
        // RelacionConPrincipal no tiene una opción "yo mismo" (aprobado explícitamente).
        public const string TitularRelacionConPrincipal = "Otro";

        // SSNs centinela usados por el sistema viejo para "no capturado" — se tratan
        // como vacíos (activan el fallback Nombre+Apellido+FechaNacimiento) en vez de
        // usarse como clave de match real (ver hallazgo: "111-11-1111"/"000-00-0000"
        // compartidos por personas distintas en el archivo real de Health).
        public static readonly HashSet<string> DummySsns = new(StringComparer.OrdinalIgnoreCase)
        {
            "111-11-1111", "000-00-0000", "123-45-6789", "999-99-9999",
        };
    }
}
