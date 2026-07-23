using ClosedXML.Excel;

namespace WholeCareInsurance.Migration.Excel
{
    // Lee la primera hoja de un .xlsx: fila 1 = headers, resto = datos.
    // Todas las celdas se leen como texto (GetString) para no perder formato
    // original (ej. "08/01/2026", "356-93-6796") — el parseo tipado vive en ExcelRow.
    public static class ExcelReader
    {
        public static List<ExcelRow> ReadRows(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheets.First();
            var usedRange = sheet.RangeUsed();
            if (usedRange == null) return new List<ExcelRow>();

            // Números de columna ABSOLUTOS (no relativos a la fila), porque el rango
            // usado no siempre arranca en la columna A — usar .Cells()/índices relativos
            // desalinea headers vs. datos si la hoja tiene algún corrimiento a la izquierda.
            var firstColNumber = usedRange.FirstColumn().ColumnNumber();
            var lastColNumber = usedRange.LastColumn().ColumnNumber();
            var headerRowNumber = usedRange.FirstRow().RowNumber();

            // El archivo real de Health repite headers literalmente ("First name" x9:
            // titular + 8 bloques de dependiente, sin sufijo propio en el .xlsx). Se
            // desambigua igual que sheet_to_json de SheetJS (usado en el análisis previo
            // a la implementación): 1ra aparición sin sufijo, 2da "_1", 3ra "_2", etc. —
            // así el resto del código (CommonFieldsExtractor/HealthDependentsExtractor,
            // que ya asumen esa convención "_1".."_8") no tiene que cambiar.
            var occurrenceCount = new Dictionary<string, int>();
            var headers = new Dictionary<int, string>();
            for (int col = firstColNumber; col <= lastColNumber; col++)
            {
                var raw = sheet.Cell(headerRowNumber, col).GetString().Trim();
                if (string.IsNullOrEmpty(raw)) { headers[col] = ""; continue; }

                var seenBefore = occurrenceCount.GetValueOrDefault(raw, 0);
                headers[col] = seenBefore == 0 ? raw : $"{raw}_{seenBefore}";
                occurrenceCount[raw] = seenBefore + 1;
            }

            var rows = new List<ExcelRow>();
            var dataRows = usedRange.RowsUsed().Skip(1);
            foreach (var row in dataRows)
            {
                var values = new Dictionary<string, string?>();
                for (int col = firstColNumber; col <= lastColNumber; col++)
                {
                    var header = headers[col];
                    if (string.IsNullOrEmpty(header)) continue;
                    var cell = sheet.Cell(row.RowNumber(), col);
                    // Se preserva la hora (Registration date / Update date la usan) — GetDate()
                    // en ExcelRow igual recorta a la parte de fecha cuando hace falta.
                    string? raw = cell.DataType == XLDataType.DateTime
                        ? cell.GetDateTime().ToString("MM/dd/yyyy hh:mm tt")
                        : cell.GetString();
                    values[header] = string.IsNullOrWhiteSpace(raw) ? null : raw;
                }
                rows.Add(new ExcelRow(values, row.RowNumber()));
            }
            return rows;
        }
    }
}
