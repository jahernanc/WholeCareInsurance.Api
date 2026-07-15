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
            if (!dto.TermsAccepted)
                return BadRequest(new ProblemDetails { Title = "Hay que aceptar los términos y condiciones." });

            var user = await _authService.Register(dto);
            if (user == null)
                return BadRequest(new ProblemDetails { Title = "El email ya está registrado." });

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

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim.Value);
            var result = await _authService.ChangePassword(userId, dto.CurrentPassword, dto.NewPassword);

            return result switch
            {
                ChangePasswordResult.Success => NoContent(),
                ChangePasswordResult.InvalidCurrentPassword => BadRequest(new ProblemDetails { Title = "La contraseña actual no es correcta." }),
                _ => Unauthorized()
            };
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _authService.ForgotPassword(dto.Email);

            // Misma respuesta exista o no el email, para no revelar qué cuentas están registradas.
            return Ok(new { message = "Si ese email está registrado, vas a recibir un link para restablecer tu contraseña." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var success = await _authService.ResetPassword(dto.Token, dto.NewPassword);
            if (!success)
                return BadRequest(new ProblemDetails { Title = "El link de restablecimiento es inválido o venció." });

            return NoContent();
        }
    }
}
