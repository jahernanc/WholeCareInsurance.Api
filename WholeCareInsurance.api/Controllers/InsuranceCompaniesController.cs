using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholeCareInsurance.api.DTOs.InsuranceCompanies;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("api/insurance-companies")]
    [Authorize]
    public class InsuranceCompaniesController : ControllerBase
    {
        private readonly IInsuranceCompanyService _companies;

        public InsuranceCompaniesController(IInsuranceCompanyService companies)
        {
            _companies = companies;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = (await _companies.GetAll()).Select(ToResponse);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var company = await _companies.GetById(id);
            if (company == null) return NotFound();
            return Ok(ToResponse(company));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] InsuranceCompanyCreateDto dto)
        {
            if (await _companies.GetByName(dto.Name) != null)
                return BadRequest(new ProblemDetails { Title = "Ya existe una aseguradora con ese nombre." });

            var created = await _companies.Create(new InsuranceCompany
            {
                Name = dto.Name,
                IsActive = dto.IsActive
            });

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] InsuranceCompanyUpdateDto dto)
        {
            var existing = await _companies.GetById(id);
            if (existing == null) return NotFound();

            var duplicate = await _companies.GetByName(dto.Name);
            if (duplicate != null && duplicate.Id != id)
                return BadRequest(new ProblemDetails { Title = "Ya existe una aseguradora con ese nombre." });

            existing.Name = dto.Name;
            existing.IsActive = dto.IsActive;

            var updated = await _companies.Update(existing);
            return Ok(ToResponse(updated));
        }

        private static InsuranceCompanyResponseDto ToResponse(InsuranceCompany c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            IsActive = c.IsActive
        };
    }
}
