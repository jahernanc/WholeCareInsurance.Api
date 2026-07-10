using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        private readonly IUsersService _users;

        public CustomersController(ICustomerService customers, IPolicyService policies, IUsersService users)
        {
            _customers = customers;
            _policies = policies;
            _users = users;
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
            int? agentId;
            int? assistantAgentId;
            int? recordAgentId;

            if (User.IsInRole("Admin"))
            {
                var error = await ValidateAgentFields(dto.AgentId, dto.AssistantAgentId, dto.RecordAgentId);
                if (error != null) return BadRequest(error);

                agentId = dto.AgentId;
                assistantAgentId = dto.AssistantAgentId;
                recordAgentId = dto.RecordAgentId;
            }
            else
            {
                agentId = CurrentUserId();
                assistantAgentId = null;
                recordAgentId = null;
            }

            var customer = MapFromDto(dto);
            customer.AgentId = agentId;
            customer.AssistantAgentId = assistantAgentId;
            customer.RecordAgentId = recordAgentId;

            var created = await _customers.Create(customer);
            var withAgents = await _customers.GetById(created.Id);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(withAgents!));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            var existing = await _customers.GetById(id);
            if (existing == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var error = await ValidateAgentFields(dto.AgentId, dto.AssistantAgentId, dto.RecordAgentId);
                if (error != null) return BadRequest(error);

                existing.AgentId = dto.AgentId;
                existing.AssistantAgentId = dto.AssistantAgentId;
                existing.RecordAgentId = dto.RecordAgentId;
            }
            // Si no es Admin, no se tocan AgentId/AssistantAgentId/RecordAgentId:
            // el usuario no ve ni puede reasignar esos campos desde el formulario.

            existing.SocialSecurityNumber = dto.SocialSecurityNumber;
            existing.FirstName = dto.FirstName;
            existing.LastName = dto.LastName;
            existing.DateOfBirth = dto.DateOfBirth;
            existing.Email = dto.Email;
            existing.Address = dto.Address;
            existing.Phone = dto.Phone;
            existing.MigrationStatus = dto.MigrationStatus;
            existing.RelacionConPrincipal = dto.RelacionConPrincipal;
            existing.ZipCode = dto.ZipCode;
            existing.State = dto.State;
            existing.City = dto.City;
            existing.County = dto.County;
            existing.MaritalStatus = dto.MaritalStatus;
            existing.Occupation = dto.Occupation;

            var updated = await _customers.Update(existing);
            var withAgents = await _customers.GetById(updated.Id);

            return Ok(ToResponse(withAgents!));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            await _customers.Delete(customer);
            return NoContent();
        }

        private int CurrentUserId()
            => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        private async Task<string?> ValidateAgentFields(int? agentId, int? assistantAgentId, int? recordAgentId)
        {
            var error = await ValidateAgent(agentId, requireEncargado: false, "Agente");
            if (error != null) return error;

            error = await ValidateAgent(assistantAgentId, requireEncargado: false, "Agente asistente");
            if (error != null) return error;

            return await ValidateAgent(recordAgentId, requireEncargado: true, "Agente record");
        }

        private async Task<string?> ValidateAgent(int? userId, bool requireEncargado, string fieldLabel)
        {
            if (!userId.HasValue) return null;

            var user = await _users.GetById(userId.Value);
            if (user == null || user.Rol != "Agente" || (requireEncargado && !user.IsEncargado))
                return $"{fieldLabel} inválido.";

            return null;
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
            ZipCode = c.ZipCode,
            State = c.State,
            City = c.City,
            County = c.County,
            MaritalStatus = c.MaritalStatus,
            Occupation = c.Occupation,
            AgentId = c.AgentId,
            AgentName = c.Agent?.Nombre,
            AssistantAgentId = c.AssistantAgentId,
            AssistantAgentName = c.AssistantAgent?.Nombre,
            RecordAgentId = c.RecordAgentId,
            RecordAgentName = c.RecordAgent?.Nombre,
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
            RelacionConPrincipal = dto.RelacionConPrincipal,
            ZipCode = dto.ZipCode,
            State = dto.State,
            City = dto.City,
            County = dto.County,
            MaritalStatus = dto.MaritalStatus,
            Occupation = dto.Occupation
        };
    }
}
