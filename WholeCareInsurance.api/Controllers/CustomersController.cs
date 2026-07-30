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
        private const int DefaultPageSize = 10;

        public CustomersController(ICustomerService customers, IPolicyService policies, IUsersService users)
        {
            _customers = customers;
            _policies = policies;
            _users = users;
        }

        // "page" es opcional a propósito: sin él devuelve el array plano de siempre
        // (lo consumen dropdowns/typeahead que necesitan la lista completa, ej. el
        // selector de dependientes/titular en Policies.jsx); con él, la pantalla de
        // administración de Customers pide una página paginada (§17).
        // "role" (§24.3, opcional): "titular" | "dependiente", filtra en ambos modos.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? page = null, [FromQuery] string? role = null)
        {
            if (!page.HasValue)
            {
                var list = (await _customers.GetAll(role)).Select(ToResponse);
                return Ok(list);
            }

            var effectivePage = page.Value < 1 ? 1 : page.Value;
            var (found, totalCount) = await _customers.Search(effectivePage, DefaultPageSize, role);
            return Ok(new PagedResponseDto<CustomerResponseDto>
            {
                Items = found.Select(ToResponse).ToList(),
                TotalCount = totalCount,
                Page = effectivePage,
                PageSize = DefaultPageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize),
            });
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
                    // InsuranceCompany faltaba en esta proyección (bug preexistente, no
                    // relacionado a los campos nuevos) — se agrega de paso.
                    InsuranceCompanyId = p.InsuranceCompanyId,
                    InsuranceCompanyName = p.InsuranceCompany.Name,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Premium = p.Premium,
                    Status = p.Status,
                    Period = p.Period,
                    NumberOfApplicants = p.NumberOfApplicants,
                    UpdatedAt = p.UpdatedAt,
                    CreatedAt = p.CreatedAt,
                    RenewalStatus = p.RenewalStatus,
                    CustomerId = p.CustomerId,
                    PlanType = p.PlanType,
                    InsurancePlan = p.InsurancePlan,
                    EffectiveDate = p.EffectiveDate,
                    TaxCreditSubsidy = p.TaxCreditSubsidy,
                    MonthlyPremiumAmount = p.MonthlyPremiumAmount
                });

            return Ok(policies);
        }

        // Dado un titular, sus dependientes vía CustomerRelationship (§24.1) — relación
        // personal, no ligada a una póliza puntual (eso lo sigue resolviendo
        // PoliciesController.GetDependents).
        [HttpGet("{id:int}/dependents")]
        public async Task<IActionResult> GetDependentsOf(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            var relationships = await _customers.GetDependentsOf(id);
            return Ok(relationships.Select(r => new CustomerRelationshipResponseDto
            {
                CustomerId = r.DependentCustomer.Id,
                FirstName = r.DependentCustomer.FirstName,
                LastName = r.DependentCustomer.LastName,
                RelationshipType = r.RelationshipType,
            }));
        }

        // Dado un dependiente, sus titulares — puede haber más de uno (§24.1, restricción
        // #1: casos confirmados de padres separados con el mismo hijo como dependiente en
        // cada póliza).
        [HttpGet("{id:int}/titulares")]
        public async Task<IActionResult> GetTitularesOf(int id)
        {
            var customer = await _customers.GetById(id);
            if (customer == null) return NotFound();

            var relationships = await _customers.GetTitularesOf(id);
            return Ok(relationships.Select(r => new CustomerRelationshipResponseDto
            {
                CustomerId = r.TitularCustomer.Id,
                FirstName = r.TitularCustomer.FirstName,
                LastName = r.TitularCustomer.LastName,
                RelationshipType = r.RelationshipType,
            }));
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
                if (error != null) return BadRequest(new ProblemDetails { Title = error });

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
                if (error != null) return BadRequest(new ProblemDetails { Title = error });

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
            existing.Address1 = dto.Address1;
            existing.Phone = dto.Phone;
            existing.MigrationStatus = dto.MigrationStatus;
            existing.RelacionConPrincipal = dto.RelacionConPrincipal;
            existing.ZipCode = dto.ZipCode;
            existing.State = dto.State;
            existing.City = dto.City;
            existing.County = dto.County;
            existing.MaritalStatus = dto.MaritalStatus;
            existing.Occupation = dto.Occupation;
            existing.MiddleName = dto.MiddleName;
            existing.Gender = dto.Gender;
            existing.GreenCard = dto.GreenCard;
            existing.WorkPermit = dto.WorkPermit;
            existing.Address2 = dto.Address2;
            existing.EmployerName = dto.EmployerName;
            existing.CompanyPhone = dto.CompanyPhone;
            existing.AnnualIncome = dto.AnnualIncome;
            existing.Tags = dto.Tags;
            existing.ContactLanguage = dto.ContactLanguage;
            existing.Age = dto.Age;
            existing.CountryOfBirth = dto.CountryOfBirth;
            existing.Height = dto.Height;
            existing.Weight = dto.Weight;
            existing.BackDateToSaveAge = dto.BackDateToSaveAge;
            existing.SpentMoreThan4MonthsAbroad = dto.SpentMoreThan4MonthsAbroad;
            existing.MilitaryOrganizationMember = dto.MilitaryOrganizationMember;
            existing.CurrentlyEmployed = dto.CurrentlyEmployed;
            existing.HasDriverLicense = dto.HasDriverLicense;
            existing.DriverLicenseNumber = dto.DriverLicenseNumber;
            existing.NetWorth = dto.NetWorth;
            existing.HouseholdIncome = dto.HouseholdIncome;
            existing.HouseholdNetWorth = dto.HouseholdNetWorth;

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
            Address1 = c.Address1,
            Phone = c.Phone,
            MigrationStatus = c.MigrationStatus,
            RelacionConPrincipal = c.RelacionConPrincipal,
            ZipCode = c.ZipCode,
            State = c.State,
            City = c.City,
            County = c.County,
            MaritalStatus = c.MaritalStatus,
            Occupation = c.Occupation,
            MiddleName = c.MiddleName,
            Gender = c.Gender,
            GreenCard = c.GreenCard,
            WorkPermit = c.WorkPermit,
            Address2 = c.Address2,
            EmployerName = c.EmployerName,
            CompanyPhone = c.CompanyPhone,
            AnnualIncome = c.AnnualIncome,
            Tags = c.Tags,
            ContactLanguage = c.ContactLanguage,
            Age = c.Age,
            CountryOfBirth = c.CountryOfBirth,
            Height = c.Height,
            Weight = c.Weight,
            BackDateToSaveAge = c.BackDateToSaveAge,
            SpentMoreThan4MonthsAbroad = c.SpentMoreThan4MonthsAbroad,
            MilitaryOrganizationMember = c.MilitaryOrganizationMember,
            CurrentlyEmployed = c.CurrentlyEmployed,
            HasDriverLicense = c.HasDriverLicense,
            DriverLicenseNumber = c.DriverLicenseNumber,
            NetWorth = c.NetWorth,
            HouseholdIncome = c.HouseholdIncome,
            HouseholdNetWorth = c.HouseholdNetWorth,
            AgentId = c.AgentId,
            AgentName = c.Agent?.Nombre,
            AgentAgency = c.Agent?.Agency,
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
            Address1 = dto.Address1,
            Phone = dto.Phone,
            MigrationStatus = dto.MigrationStatus,
            RelacionConPrincipal = dto.RelacionConPrincipal,
            ZipCode = dto.ZipCode,
            State = dto.State,
            City = dto.City,
            County = dto.County,
            MaritalStatus = dto.MaritalStatus,
            Occupation = dto.Occupation,
            MiddleName = dto.MiddleName,
            Gender = dto.Gender,
            GreenCard = dto.GreenCard,
            WorkPermit = dto.WorkPermit,
            Address2 = dto.Address2,
            EmployerName = dto.EmployerName,
            CompanyPhone = dto.CompanyPhone,
            AnnualIncome = dto.AnnualIncome,
            Tags = dto.Tags,
            ContactLanguage = dto.ContactLanguage,
            Age = dto.Age,
            CountryOfBirth = dto.CountryOfBirth,
            Height = dto.Height,
            Weight = dto.Weight,
            BackDateToSaveAge = dto.BackDateToSaveAge,
            SpentMoreThan4MonthsAbroad = dto.SpentMoreThan4MonthsAbroad,
            MilitaryOrganizationMember = dto.MilitaryOrganizationMember,
            CurrentlyEmployed = dto.CurrentlyEmployed,
            HasDriverLicense = dto.HasDriverLicense,
            DriverLicenseNumber = dto.DriverLicenseNumber,
            NetWorth = dto.NetWorth,
            HouseholdIncome = dto.HouseholdIncome,
            HouseholdNetWorth = dto.HouseholdNetWorth
        };
    }
}
