using System.Globalization;

namespace WholeCareInsurance.Migration.Excel
{
    // Envoltorio liviano sobre una fila del reporte (header -> valor de celda como string).
    // Todas las columnas del origen llegan como texto; el parseo de fecha/decimal/int
    // se hace acá para no repetir Convert.ToXxx en cada importer.
    public class ExcelRow
    {
        private readonly Dictionary<string, string?> _values;
        public int RowNumber { get; }

        public ExcelRow(Dictionary<string, string?> values, int rowNumber)
        {
            _values = values;
            RowNumber = rowNumber;
        }

        public string? GetString(string column)
        {
            if (!_values.TryGetValue(column, out var v)) return null;
            if (string.IsNullOrWhiteSpace(v)) return null;
            return v.Trim();
        }

        public bool Has(string column) => _values.ContainsKey(column);

        // Formatos observados en los 4 archivos reales: "MM/DD/YYYY" y "MM/DD/YYYY HH:MM AM/PM".
        public DateTime? GetDate(string column)
        {
            var s = GetString(column);
            if (s == null) return null;
            var datePart = s.Split(' ')[0];
            if (DateTime.TryParseExact(datePart, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                return d2;
            return null;
        }

        public DateTime? GetDateTime(string column)
        {
            var s = GetString(column);
            if (s == null) return null;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            return GetDate(column);
        }

        public decimal? GetDecimal(string column)
        {
            var s = GetString(column);
            if (s == null) return null;
            var cleaned = s.Replace("$", "").Replace(",", "").Trim();
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        public int? GetInt(string column)
        {
            var s = GetString(column);
            if (s == null) return null;
            return int.TryParse(s, out var i) ? i : null;
        }
    }
}
