using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
        public async Task<IActionResult> GetAll([FromQuery] int? customerId = null)
        {
            var all = await _policies.GetAll();

            var list = (customerId.HasValue ? all.Where(p => p.CustomerId == customerId.Value) : all)
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
                    SocialSecurityNumber = pd.Customer.SocialSecurityNumber
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
                CustomerId = dto.CustomerId
            });

            return CreatedAtAction(nameof(GetDependents), new { id }, new DependentResponseDto
            {
                CustomerId = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                SocialSecurityNumber = customer.SocialSecurityNumber
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

        public class PolicyCreateDto
        {
            public string PolicyNumber { get; set; } = default!;

            [Required]
            [AllowedValues("Obama Care", "Salud", "Auto", "Otro",
                ErrorMessage = "Tipo de póliza inválido.")]
            public string Type { get; set; } = default!;

            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public decimal Premium { get; set; }
            public string Status { get; set; } = "Active";
            public int CustomerId { get; set; }
        }

        public class PolicyUpdateDto : PolicyCreateDto { }

        public class PolicyResponseDto
        {
            public int Id { get; set; }
            public string PolicyNumber { get; set; } = default!;
            public string Type { get; set; } = default!;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public decimal Premium { get; set; }
            public string Status { get; set; } = default!;
            public int CustomerId { get; set; }
        }

        public class DependentCreateDto
        {
            [Required] public int CustomerId { get; set; }
        }

        public class DependentResponseDto
        {
            public int CustomerId { get; set; }
            public string FirstName { get; set; } = default!;
            public string LastName { get; set; } = default!;
            public string SocialSecurityNumber { get; set; } = default!;
        }
    }
}
