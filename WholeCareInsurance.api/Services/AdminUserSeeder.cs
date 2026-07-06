using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class AdminUserSeeder
    {
        private readonly IUsersService _usersService;

        public AdminUserSeeder(IUsersService usersService)
        {
            _usersService = usersService;
        }

        public async Task Seed()
        {
            const string adminEmail = "admin@wholecare.com";

            if (await _usersService.GetByEmail(adminEmail) != null)
                return;

            var adminUser = new User
            {
                Nombre = "Administrador",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Rol = "Admin"
            };

            await _usersService.Create(adminUser);
        }
    }
}
