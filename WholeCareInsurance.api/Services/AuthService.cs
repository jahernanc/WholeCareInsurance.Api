using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WholeCareInsurance.api.DTOs.Auth;
using WholeCareInsurance.api.DTOs.Users;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly IUsersService _usersService;
        private readonly IEmailService _emailService;

        public AuthService(IConfiguration config, IUsersService usersService, IEmailService emailService)
        {
            _config = config;
            _usersService = usersService;
            _emailService = emailService;
        }

        public async Task<UserResponseDto?> Register(AuthRegisterDto dto)
        {
            if (await _usersService.GetByEmail(dto.Email) != null)
                return null;

            var user = new User
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = dto.Rol,
                IsEncargado = dto.IsEncargado,
                // El Admin le asigna la contraseña inicial: se obliga a cambiarla en el primer login.
                MustChangePassword = true,

                MiddleName = dto.MiddleName,
                Gender = dto.Gender,
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                City = dto.City,
                ZipCode = dto.ZipCode,
                State = dto.State,
                County = dto.County,
                Licensed = dto.Licensed,
                LicenseNumber = dto.LicenseNumber,
                NpnNumber = dto.NpnNumber,
                NpnOverride = dto.NpnOverride,
                HasCompanyContract = dto.HasCompanyContract,
                ContractNumber = dto.ContractNumber,
                CompanyName = dto.CompanyName,
                ContractsWanted = dto.ContractsWanted,
                AdditionalInformation = dto.AdditionalInformation,
                // dto.TermsAccepted ya se validó como true en el controller antes de llegar acá.
                TermsAccepted = dto.TermsAccepted,
                TermsAcceptedAt = DateTime.UtcNow
            };

            var created = await _usersService.Create(user);

            return new UserResponseDto
            {
                Id = created.Id,
                Nombre = created.Nombre,
                Email = created.Email,
                Rol = created.Rol,
                IsEncargado = created.IsEncargado,
                PreferredLanguage = created.PreferredLanguage,
                MiddleName = created.MiddleName,
                Gender = created.Gender,
                Address1 = created.Address1,
                Address2 = created.Address2,
                City = created.City,
                ZipCode = created.ZipCode,
                State = created.State,
                County = created.County,
                Licensed = created.Licensed,
                LicenseNumber = created.LicenseNumber,
                NpnNumber = created.NpnNumber,
                NpnOverride = created.NpnOverride,
                HasCompanyContract = created.HasCompanyContract,
                ContractNumber = created.ContractNumber,
                CompanyName = created.CompanyName,
                ContractsWanted = created.ContractsWanted,
                AdditionalInformation = created.AdditionalInformation,
                TermsAccepted = created.TermsAccepted,
                TermsAcceptedAt = created.TermsAcceptedAt
            };
        }

        public async Task<AuthResponseDto?> Login(AuthLoginDto dto)
        {
            var user = await _usersService.GetByEmail(dto.Email);
            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            var refreshToken = GenerateRandomToken();
            user.RefreshTokenHash = HashToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _usersService.Update(user);

            return new AuthResponseDto
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = refreshToken,
                PreferredLanguage = user.PreferredLanguage,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task<AuthResponseDto?> Refresh(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var user = await _usersService.GetByRefreshTokenHash(hash);

            if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
                return null;

            var newRefreshToken = GenerateRandomToken();
            user.RefreshTokenHash = HashToken(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _usersService.Update(user);

            return new AuthResponseDto
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = newRefreshToken,
                PreferredLanguage = user.PreferredLanguage,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task Logout(int userId)
        {
            var user = await _usersService.GetById(userId);
            if (user == null)
                return;

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _usersService.Update(user);
        }

        public async Task<ChangePasswordResult> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = await _usersService.GetById(userId);
            if (user == null)
                return ChangePasswordResult.UserNotFound;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return ChangePasswordResult.InvalidCurrentPassword;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = false;

            // Fuerza a re-loguearse en cualquier otra sesión activa (esta app solo
            // guarda un refresh token por usuario, así que esto invalida "el resto").
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;

            await _usersService.Update(user);
            return ChangePasswordResult.Success;
        }

        public async Task ForgotPassword(string email)
        {
            var user = await _usersService.GetByEmail(email);
            // No revelar si el email existe o no: mismo comportamiento (silencioso) en ambos casos.
            if (user == null)
                return;

            // Mitigación liviana anti-spam: si ya hay un token vigente, no se genera uno
            // nuevo ni se reenvía el email — evita que pedir el link repetidas veces
            // dispare un email cada vez.
            if (user.PasswordResetTokenExpiresAt.HasValue && user.PasswordResetTokenExpiresAt > DateTime.UtcNow)
                return;

            var resetToken = GenerateRandomToken();
            user.PasswordResetTokenHash = HashToken(resetToken);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _usersService.Update(user);

            var baseUrl = _config["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
            var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

            var body = $"""
                <p>Hola {System.Net.WebUtility.HtmlEncode(user.Nombre)},</p>
                <p>Recibimos una solicitud para restablecer tu contraseña de WholeCare Insurance.</p>
                <p><a href="{resetLink}">Hacé clic acá para elegir una nueva contraseña</a></p>
                <p>Este link vence en 1 hora. Si no pediste este cambio, podés ignorar este email.</p>
                """;

            await _emailService.SendAsync(user.Email, "Restablecer tu contraseña — WholeCare Insurance", body);
        }

        public async Task<bool> ResetPassword(string token, string newPassword)
        {
            var hash = HashToken(token);
            var user = await _usersService.GetByPasswordResetTokenHash(hash);

            if (user == null || user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = false;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;

            // Mismo criterio que ChangePassword: invalida cualquier sesión activa.
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;

            await _usersService.Update(user);
            return true;
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                // Redundante con Sub a propósito: el mapeo por default de JwtSecurityTokenHandler
                // ya traduce "sub" -> ClaimTypes.NameIdentifier, pero se deja explícito para no
                // depender de ese comportamiento implícito del framework.
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.Nombre),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Rol)
            };

            var minutes = _config.GetValue<int>("Jwt:AccessTokenMinutes", 60);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Antes usaba Guid.NewGuid() (no es un generador criptográficamente aleatorio).
        // Se usa el mismo generador tanto para refresh tokens como para tokens de reset.
        private static string GenerateRandomToken()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
