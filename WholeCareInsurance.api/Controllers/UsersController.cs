using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WholeCareInsurance.api.DTOs.Users;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;
using WholeCareInsurance.api.Utils;

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

            var conditionalFieldsError = AgentFieldValidation.Validate(dto.Licensed, dto.LicenseNumber, dto.HasCompanyContract, dto.ContractNumber, dto.CompanyName);
            if (conditionalFieldsError != null)
                return BadRequest(new ProblemDetails { Title = conditionalFieldsError });

            existing.Nombre = dto.Nombre;
            existing.Email = dto.Email;
            existing.Rol = dto.Rol;
            existing.IsEncargado = dto.IsEncargado;

            existing.MiddleName = dto.MiddleName;
            existing.Gender = dto.Gender;
            existing.Address1 = dto.Address1;
            existing.Address2 = dto.Address2;
            existing.City = dto.City;
            existing.ZipCode = dto.ZipCode;
            existing.State = dto.State;
            existing.County = dto.County;
            existing.Licensed = dto.Licensed;
            // No persistir un valor suelto si el flag correspondiente está en false
            // (mismo criterio que ya aplica el frontend antes de enviar el formulario).
            existing.LicenseNumber = dto.Licensed ? dto.LicenseNumber : null;
            existing.NpnNumber = dto.NpnNumber;
            existing.NpnOverride = dto.NpnOverride;
            existing.HasCompanyContract = dto.HasCompanyContract;
            existing.ContractNumber = dto.HasCompanyContract ? dto.ContractNumber : null;
            existing.CompanyName = dto.HasCompanyContract ? dto.CompanyName : null;
            existing.ContractsWanted = dto.ContractsWanted;
            existing.AdditionalInformation = dto.AdditionalInformation;

            // Solo se pisa TermsAccepted si pasa a true (nunca se "desacepta" desde una edición).
            if (dto.TermsAccepted && !existing.TermsAccepted)
            {
                existing.TermsAccepted = true;
                existing.TermsAcceptedAt = DateTime.UtcNow;
            }

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
            PreferredLanguage = u.PreferredLanguage,
            MiddleName = u.MiddleName,
            Gender = u.Gender,
            Address1 = u.Address1,
            Address2 = u.Address2,
            City = u.City,
            ZipCode = u.ZipCode,
            State = u.State,
            County = u.County,
            Licensed = u.Licensed,
            LicenseNumber = u.LicenseNumber,
            NpnNumber = u.NpnNumber,
            NpnOverride = u.NpnOverride,
            HasCompanyContract = u.HasCompanyContract,
            ContractNumber = u.ContractNumber,
            CompanyName = u.CompanyName,
            ContractsWanted = u.ContractsWanted,
            AdditionalInformation = u.AdditionalInformation,
            TermsAccepted = u.TermsAccepted,
            TermsAcceptedAt = u.TermsAcceptedAt
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
