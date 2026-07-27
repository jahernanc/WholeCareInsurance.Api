using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboard;

        public DashboardController(IDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        private int CurrentUserId()
            => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        // Único punto donde se decide a qué agente se scopea la consulta. Admin: usa lo que
        // mande (o null = vista global). Agente: SIEMPRE su propio Id, sin importar qué
        // agentId venga en el query string — no hay que confiar en el frontend para esto,
        // alguien podría mandarlo a mano por la URL para intentar ver datos de otro agente.
        private int? ResolveEffectiveAgentId(int? requestedAgentId)
            => User.IsInRole("Admin") ? requestedAgentId : CurrentUserId();

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int? agentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
            => Ok(await _dashboard.GetSummary(ResolveEffectiveAgentId(agentId), from, to));

        [HttpGet("by-status")]
        public async Task<IActionResult> GetByStatus([FromQuery] int? agentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
            => Ok(await _dashboard.GetByStatus(ResolveEffectiveAgentId(agentId), from, to));

        [HttpGet("by-type")]
        public async Task<IActionResult> GetByType([FromQuery] int? agentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
            => Ok(await _dashboard.GetByType(ResolveEffectiveAgentId(agentId), from, to));

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int? agentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
            => Ok(await _dashboard.GetStats(ResolveEffectiveAgentId(agentId), from, to));

        [HttpGet("latest-policies")]
        public async Task<IActionResult> GetLatestPolicies([FromQuery] int? agentId)
            => Ok(await _dashboard.GetLatestPolicies(ResolveEffectiveAgentId(agentId)));

        [HttpGet("upcoming-65")]
        public async Task<IActionResult> GetUpcoming65([FromQuery] int? agentId)
            => Ok(await _dashboard.GetUpcoming65(ResolveEffectiveAgentId(agentId)));
    }
}
