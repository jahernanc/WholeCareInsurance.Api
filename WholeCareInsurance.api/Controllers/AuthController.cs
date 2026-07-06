using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WholeCareInsurance.api.DTOs.Auth;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthRegisterDto dto)
        {
            var user = await _authService.Register(dto);
            if (user == null)
                return BadRequest("El email ya está registrado.");

            return Ok(new { message = "Usuario registrado correctamente", user });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthLoginDto dto)
        {
            var token = await _authService.Login(dto);
            if (token == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(new { token });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(AuthRefreshDto dto)
        {
            var result = await _authService.Refresh(dto.RefreshToken);
            if (result == null)
                return Unauthorized("Refresh token inválido o expirado.");

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim.Value);
            await _authService.Logout(userId);

            return NoContent();
        }
    }
}
