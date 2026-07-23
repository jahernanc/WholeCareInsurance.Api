namespace WholeCareInsurance.Migration.Lookups
{
    // Portado de wholecare-admin-vs/src/data/usStates.js — mismo catálogo que usa
    // el <select> de Customer.State en el frontend, para que el código migrado
    // caiga en los mismos 2-letter codes que ya acepta el sistema.
    public static class UsStates
    {
        private static readonly Dictionary<string, string> ByFullName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Alabama"] = "AL", ["Alaska"] = "AK", ["Arizona"] = "AZ", ["Arkansas"] = "AR",
            ["California"] = "CA", ["Colorado"] = "CO", ["Connecticut"] = "CT", ["Delaware"] = "DE",
            ["Florida"] = "FL", ["Georgia"] = "GA", ["Hawaii"] = "HI", ["Idaho"] = "ID",
            ["Illinois"] = "IL", ["Indiana"] = "IN", ["Iowa"] = "IA", ["Kansas"] = "KS",
            ["Kentucky"] = "KY", ["Louisiana"] = "LA", ["Maine"] = "ME", ["Maryland"] = "MD",
            ["Massachusetts"] = "MA", ["Michigan"] = "MI", ["Minnesota"] = "MN", ["Mississippi"] = "MS",
            ["Missouri"] = "MO", ["Montana"] = "MT", ["Nebraska"] = "NE", ["Nevada"] = "NV",
            ["New Hampshire"] = "NH", ["New Jersey"] = "NJ", ["New Mexico"] = "NM", ["New York"] = "NY",
            ["North Carolina"] = "NC", ["North Dakota"] = "ND", ["Ohio"] = "OH", ["Oklahoma"] = "OK",
            ["Oregon"] = "OR", ["Pennsylvania"] = "PA", ["Rhode Island"] = "RI", ["South Carolina"] = "SC",
            ["South Dakota"] = "SD", ["Tennessee"] = "TN", ["Texas"] = "TX", ["Utah"] = "UT",
            ["Vermont"] = "VT", ["Virginia"] = "VA", ["Washington"] = "WA", ["West Virginia"] = "WV",
            ["Wisconsin"] = "WI", ["Wyoming"] = "WY", ["District of Columbia"] = "DC",
        };

        // Devuelve null si no matchea (ya sea nombre completo o directamente un
        // código de 2 letras válido) — el llamador decide si eso es un warning.
        public static string? ToCode(string? fullNameOrCode)
        {
            if (string.IsNullOrWhiteSpace(fullNameOrCode)) return null;
            var trimmed = fullNameOrCode.Trim();
            if (trimmed.Length == 2 && ByFullName.Values.Contains(trimmed.ToUpperInvariant()))
                return trimmed.ToUpperInvariant();
            return ByFullName.TryGetValue(trimmed, out var code) ? code : null;
        }
    }
}
