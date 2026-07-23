namespace WholeCareInsurance.Migration
{
    public class CliOptions
    {
        public bool Commit { get; private set; }
        public bool Confirmed { get; private set; }
        public string SourceDir { get; private set; } = default!;
        public int HistoryWindowDays { get; private set; } = 200;
        public string? ConnectionString { get; private set; }

        public static CliOptions? Parse(string[] args)
        {
            var options = new CliOptions { SourceDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "migration-source") };
            bool dryRun = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--dry-run": dryRun = true; break;
                    case "--commit": options.Commit = true; break;
                    case "--confirm": options.Confirmed = true; break;
                    case "--source": options.SourceDir = args[++i]; break;
                    case "--history-window-days": options.HistoryWindowDays = int.Parse(args[++i]); break;
                    case "--connection": options.ConnectionString = args[++i]; break;
                    default:
                        Console.Error.WriteLine($"Argumento desconocido: {args[i]}");
                        PrintUsage();
                        return null;
                }
            }

            if (!dryRun && !options.Commit)
            {
                Console.Error.WriteLine("Hay que indicar --dry-run o --commit.");
                PrintUsage();
                return null;
            }

            if (options.Commit && !options.Confirmed)
            {
                Console.Error.WriteLine(
                    "--commit requiere además --confirm explícito (correr --dry-run primero y revisar el reporte).");
                return null;
            }

            options.SourceDir = Path.GetFullPath(options.SourceDir);
            if (!Directory.Exists(options.SourceDir))
            {
                Console.Error.WriteLine($"No existe el directorio de origen: {options.SourceDir}");
                return null;
            }

            return options;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("""
                Uso:
                  WholeCareInsurance.Migration --dry-run [--source <dir>] [--history-window-days N] [--connection "<connstr>"]
                  WholeCareInsurance.Migration --commit --confirm [--source <dir>] [--history-window-days N] [--connection "<connstr>"]

                --dry-run                 Simula todo el proceso, no escribe nada en la base, genera el reporte.
                --commit --confirm        Ejecuta la migración real (requiere --confirm explícito).
                --source <dir>            Carpeta con los 4 .xlsx (default: ../migration-source).
                --history-window-days N   Ventana de consolidación de historial en días (default: 200).
                --connection "<connstr>"  Connection string explícita (default: la de WholeCareInsurance.api/appsettings*.json).
                """);
        }
    }
}
