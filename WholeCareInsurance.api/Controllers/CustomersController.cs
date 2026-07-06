using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var list = (await _customers.GetAll())
                .Select(c => new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    DocumentNumber = c.DocumentNumber,
                    PoliciesCount = c.Policies?.Count ?? 0
                });

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            return Ok(new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                DocumentNumber = customer.DocumentNumber,
                PoliciesCount = customer.Policies?.Count ?? 0
            });
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
            var customer = new Customer
            {
                Name = dto.Name,
                Email = dto.Email,
                DocumentNumber = dto.DocumentNumber
            };

            var created = await _customers.Create(customer);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new CustomerResponseDto
            {
                Id = created.Id,
                Name = created.Name,
                Email = created.Email,
                DocumentNumber = created.DocumentNumber,
                PoliciesCount = 0
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            var existing = await _customers.GetById(id);
            if (existing == null) return NotFound();

            existing.Name = dto.Name;
            existing.Email = dto.Email;
            existing.DocumentNumber = dto.DocumentNumber;

            var updated = await _customers.Update(existing);

            return Ok(new CustomerResponseDto
            {
                Id = updated.Id,
                Name = updated.Name,
                Email = updated.Email,
                DocumentNumber = updated.DocumentNumber,
                PoliciesCount = updated.Policies?.Count ?? 0
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            await _customers.Delete(customer);
            return NoContent();
        }

        public class CustomerCreateDto
        {
            public string Name { get; set; } = default!;
            public string Email { get; set; } = default!;
            public string DocumentNumber { get; set; } = default!;
        }

        public class CustomerUpdateDto : CustomerCreateDto { }

        public class CustomerResponseDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
            public string Email { get; set; } = default!;
            public string DocumentNumber { get; set; } = default!;
            public int PoliciesCount { get; set; }
        }

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
    }
}
