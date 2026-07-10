using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholeCareInsurance.api.DTOs.Customers;
using WholeCareInsurance.api.DTOs.Policies;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customers;
        private readonly IPolicyService _policies;

        public CustomersController(ICustomerService customers, IPolicyService policies)
        {
            _customers = customers;
            _policies = policies;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = (await _customers.GetAll()).Select(ToResponse);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();
            return Ok(ToResponse(customer));
        }

        [HttpGet("{id:int}/policies")]
        public async Task<IActionResult> GetPoliciesForCustomer(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            var policies = (await _policies.GetAll())
                .Where(p => p.CustomerId == id)
                .Select(p => new PolicyResponseDto
                {
                    Id = p.Id,
                    PolicyNumber = p.PolicyNumber,
                    Type = p.Type,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Premium = p.Premium,
                    Status = p.Status,
                    CustomerId = p.CustomerId
                });

            return Ok(policies);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            var customer = MapFromDto(dto);
            var created = await _customers.Create(customer);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            var existing = await _customers.GetById(id);
            if (existing == null) return NotFound();

            existing.SocialSecurityNumber = dto.SocialSecurityNumber;
            existing.FirstName = dto.FirstName;
            existing.LastName = dto.LastName;
            existing.DateOfBirth = dto.DateOfBirth;
            existing.Email = dto.Email;
            existing.Address = dto.Address;
            existing.Phone = dto.Phone;
            existing.MigrationStatus = dto.MigrationStatus;
            existing.RelacionConPrincipal = dto.RelacionConPrincipal;

            var updated = await _customers.Update(existing);
            return Ok(ToResponse(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            await _customers.Delete(customer);
            return NoContent();
        }

        private static CustomerResponseDto ToResponse(Customer c) => new()
        {
            Id = c.Id,
            SocialSecurityNumber = c.SocialSecurityNumber,
            FirstName = c.FirstName,
            LastName = c.LastName,
            DateOfBirth = c.DateOfBirth,
            Email = c.Email,
            Address = c.Address,
            Phone = c.Phone,
            MigrationStatus = c.MigrationStatus,
            RelacionConPrincipal = c.RelacionConPrincipal,
            PoliciesCount = c.Policies?.Count ?? 0
        };

        private static Customer MapFromDto(CustomerCreateDto dto) => new()
        {
            SocialSecurityNumber = dto.SocialSecurityNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            Address = dto.Address,
            Phone = dto.Phone,
            MigrationStatus = dto.MigrationStatus,
            RelacionConPrincipal = dto.RelacionConPrincipal
        };

        private static readonly string[] ValidMigrationStatuses =
            ["Permiso de trabajo", "Residente permanente", "Ciudadano", "Otro"];
    }
}
