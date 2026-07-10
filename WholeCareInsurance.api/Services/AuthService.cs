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

        public AuthService(IConfiguration config, IUsersService usersService)
        {
            _config = config;
            _usersService = usersService;
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
                IsEncargado = dto.IsEncargado
            };

            var created = await _usersService.Create(user);

            return new UserResponseDto
            {
                Id = created.Id,
                Nombre = created.Nombre,
                Email = created.Email,
                Rol = created.Rol,
                IsEncargado = created.IsEncargado
            };
        }

        public async Task<AuthResponseDto?> Login(AuthLoginDto dto)
        {
            var user = await _usersService.GetByEmail(dto.Email);
            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            var refreshToken = GenerateRefreshToken();
            user.RefreshTokenHash = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _usersService.Update(user);

            return new AuthResponseDto
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponseDto?> Refresh(string refreshToken)
        {
            var hash = HashRefreshToken(refreshToken);
            var user = await _usersService.GetByRefreshTokenHash(hash);

            if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
                return null;

            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokenHash = HashRefreshToken(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _usersService.Update(user);

            return new AuthResponseDto
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = newRefreshToken
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

        private static string HashRefreshToken(string token)
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

        private static string GenerateRefreshToken()
            => Guid.NewGuid().ToString("N");
    }
}
