using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WholeCareInsurance.api.DTOs.Users;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] string? role = null)
        {
            var users = await _usersService.GetAll();

            if (!string.IsNullOrWhiteSpace(role))
                users = users.Where(u => u.Rol == role);

            return Ok(users.Select(ToResponse));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _usersService.GetById(id);
            if (user == null) return NotFound();
            return Ok(ToResponse(user));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var email = User.Identity!.Name!;
            var user = await _usersService.GetByEmail(email);
            if (user == null) return NotFound();
            return Ok(ToMeResponse(user));
        }

        [HttpPut("me/language")]
        [Authorize]
        public async Task<IActionResult> UpdateMyLanguage([FromBody] UpdateLanguageDto dto)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var user = await _usersService.GetById(userId);
            if (user == null) return NotFound();

            user.PreferredLanguage = dto.Language;
            await _usersService.Update(user);

            return NoContent();
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
        {
            var existing = await _usersService.GetById(id);
            if (existing == null) return NotFound();

            existing.Nombre = dto.Nombre;
            existing.Email = dto.Email;
            existing.Rol = dto.Rol;
            existing.IsEncargado = dto.IsEncargado;

            var updated = await _usersService.Update(existing);
            return Ok(ToResponse(updated));
        }

        private static UserResponseDto ToResponse(User u) => new()
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Email = u.Email,
            Rol = u.Rol,
            IsEncargado = u.IsEncargado,
            PreferredLanguage = u.PreferredLanguage
        };

        private static UserMeDto ToMeResponse(User u) => new()
        {
            Nombre = u.Nombre,
            Email = u.Email,
            Rol = u.Rol,
            IsEncargado = u.IsEncargado,
            PreferredLanguage = u.PreferredLanguage,
            MustChangePassword = u.MustChangePassword
        };
    }
}
