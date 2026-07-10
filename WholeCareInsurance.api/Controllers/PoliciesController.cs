using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholeCareInsurance.api.DTOs.Policies;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("api/policies")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policies;
        private readonly ICustomerService _customers;

        public PoliciesController(IPolicyService policies, ICustomerService customers)
        {
            _policies = policies;
            _customers = customers;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? customerId = null,
            [FromQuery] string? firstName = null,
            [FromQuery] string? lastName = null,
            [FromQuery] string? policyNumber = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] string? insuranceCompany = null)
        {
            var found = await _policies.Search(customerId, firstName, lastName, policyNumber, status, type, insuranceCompany);

            var list = found.Select(p => new PolicyResponseDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                Type = p.Type,
                InsuranceCompany = p.InsuranceCompany,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Premium = p.Premium,
                Status = p.Status,
                CustomerId = p.CustomerId
            });

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            return Ok(new PolicyResponseDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                Type = policy.Type,
                InsuranceCompany = policy.InsuranceCompany,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                Premium = policy.Premium,
                Status = policy.Status,
                CustomerId = policy.CustomerId
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyCreateDto dto)
        {
            var customer = await _customers.GetById(dto.CustomerId);
            if (customer == null)
                return BadRequest($"CustomerId {dto.CustomerId} no existe.");

            var policy = new Policy
            {
                PolicyNumber = dto.PolicyNumber,
                Type = dto.Type,
                InsuranceCompany = dto.InsuranceCompany,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Premium = dto.Premium,
                Status = dto.Status,
                CustomerId = dto.CustomerId
            };

            var created = await _policies.Create(policy);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new PolicyResponseDto
            {
                Id = created.Id,
                PolicyNumber = created.PolicyNumber,
                Type = created.Type,
                InsuranceCompany = created.InsuranceCompany,
                StartDate = created.StartDate,
                EndDate = created.EndDate,
                Premium = created.Premium,
                Status = created.Status,
                CustomerId = created.CustomerId
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PolicyUpdateDto dto)
        {
            var existing = await _policies.GetById(id);
            if (existing == null) return NotFound();

            if (dto.CustomerId != existing.CustomerId)
            {
                var customer = await _customers.GetById(dto.CustomerId);
                if (customer == null)
                    return BadRequest($"CustomerId {dto.CustomerId} no existe.");
                existing.CustomerId = dto.CustomerId;
            }

            existing.PolicyNumber = dto.PolicyNumber;
            existing.Type = dto.Type;
            existing.InsuranceCompany = dto.InsuranceCompany;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.Premium = dto.Premium;
            existing.Status = dto.Status;

            var updated = await _policies.Update(existing);

            return Ok(new PolicyResponseDto
            {
                Id = updated.Id,
                PolicyNumber = updated.PolicyNumber,
                Type = updated.Type,
                InsuranceCompany = updated.InsuranceCompany,
                StartDate = updated.StartDate,
                EndDate = updated.EndDate,
                Premium = updated.Premium,
                Status = updated.Status,
                CustomerId = updated.CustomerId
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            await _policies.Delete(policy);
            return NoContent();
        }

        [HttpGet("{id:int}/dependents")]
        public async Task<IActionResult> GetDependents(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var dependents = (await _policies.GetDependents(id))
                .Select(pd => new DependentResponseDto
                {
                    CustomerId = pd.Customer.Id,
                    FirstName = pd.Customer.FirstName,
                    LastName = pd.Customer.LastName,
                    SocialSecurityNumber = pd.Customer.SocialSecurityNumber,
                    IsAplicante = pd.IsAplicante
                });

            return Ok(dependents);
        }

        [HttpPost("{id:int}/dependents")]
        public async Task<IActionResult> AddDependent(int id, [FromBody] DependentCreateDto dto)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var customer = await _customers.GetById(dto.CustomerId);
            if (customer == null)
                return BadRequest($"CustomerId {dto.CustomerId} no existe.");

            if (dto.CustomerId == policy.CustomerId)
                return BadRequest("El titular no puede ser su propio dependiente.");

            if (await _policies.GetDependent(id, dto.CustomerId) != null)
                return BadRequest("Ya es dependiente de esta póliza.");

            var created = await _policies.AddDependent(new PolicyDependent
            {
                PolicyId = id,
                CustomerId = dto.CustomerId,
                IsAplicante = dto.IsAplicante
            });

            return CreatedAtAction(nameof(GetDependents), new { id }, new DependentResponseDto
            {
                CustomerId = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                SocialSecurityNumber = customer.SocialSecurityNumber,
                IsAplicante = created.IsAplicante
            });
        }

        [HttpPut("{id:int}/dependents/{customerId:int}")]
        public async Task<IActionResult> UpdateDependent(int id, int customerId, [FromBody] DependentUpdateDto dto)
        {
            var dependent = await _policies.GetDependent(id, customerId);
            if (dependent == null) return NotFound();

            dependent.IsAplicante = dto.IsAplicante;
            var updated = await _policies.UpdateDependent(dependent);

            return Ok(new DependentResponseDto
            {
                CustomerId = updated.Customer.Id,
                FirstName = updated.Customer.FirstName,
                LastName = updated.Customer.LastName,
                SocialSecurityNumber = updated.Customer.SocialSecurityNumber,
                IsAplicante = updated.IsAplicante
            });
        }

        [HttpDelete("{id:int}/dependents/{customerId:int}")]
        public async Task<IActionResult> RemoveDependent(int id, int customerId)
        {
            var dependent = await _policies.GetDependent(id, customerId);
            if (dependent == null) return NotFound();

            await _policies.RemoveDependent(dependent);
            return NoContent();
        }
    }
}
