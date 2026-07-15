using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class AdminUserSeeder
    {
        private readonly IUsersService _usersService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminUserSeeder> _logger;

        public AdminUserSeeder(IUsersService usersService, IConfiguration configuration, ILogger<AdminUserSeeder> logger)
        {
            _usersService = usersService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Seed()
        {
            // Admin__FirstName / Admin__LastName / Admin__Email / Admin__InitialPassword
            // (ver README.md, sección "Despliegue") permiten seedear un admin real por
            // ambiente. Cada una cae de forma independiente al valor default hardcodeado
            // de siempre si no está seteada — así el flujo de dev local no cambia si nadie
            // configura nada, pero Test/Prod pueden pisar solo lo que necesiten.
            var firstName = _configuration["Admin:FirstName"];
            var lastName = _configuration["Admin:LastName"];
            var email = _configuration["Admin:Email"];
            var initialPassword = _configuration["Admin:InitialPassword"];

            var usingDefaults = string.IsNullOrWhiteSpace(firstName)
                && string.IsNullOrWhiteSpace(lastName)
                && string.IsNullOrWhiteSpace(email)
                && string.IsNullOrWhiteSpace(initialPassword);

            var nombre = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)
                ? "Administrador"
                : $"{firstName} {lastName}".Trim();

            var resolvedEmail = string.IsNullOrWhiteSpace(email) ? "admin@wholecare.com" : email;
            var resolvedPassword = string.IsNullOrWhiteSpace(initialPassword) ? "Admin123!" : initialPassword;

            if (await _usersService.GetByEmail(resolvedEmail) != null)
                return;

            if (usingDefaults)
                _logger.LogWarning(
                    "Admin__* no configurado — seedeando el admin default ({Email}). Configurar Admin__FirstName/Admin__LastName/Admin__Email/Admin__InitialPassword antes de un despliegue real.",
                    resolvedEmail);
            else
                _logger.LogInformation("Seedeando admin inicial desde configuración: {Email}", resolvedEmail);

            var adminUser = new User
            {
                Nombre = nombre,
                Email = resolvedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(resolvedPassword),
                Rol = "Admin",
                // Sin importar la password inicial (default o configurada), se obliga a
                // cambiarla en el primer login, igual que cualquier usuario nuevo creado por un Admin.
                MustChangePassword = true
            };

            await _usersService.Create(adminUser);
        }
    }
}
