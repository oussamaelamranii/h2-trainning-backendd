using System.Security.Claims;
using H2_Trainning.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H2_Trainning.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("coach")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachDashboard()
        {
            var dashboard = await _service.GetCoachDashboardAsync(GetUserId());
            return Ok(dashboard);
        }

        [HttpGet("client")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetClientDashboard()
        {
            var userId = GetUserId();
            await _service.UpdateLastCheckedAsync(userId);
            var dashboard = await _service.GetClientDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetClientDashboardForCoach(string clientId)
        {
            var dashboard = await _service.GetClientDashboardAsync(clientId);
            if (dashboard?.CurrentProgram != null && dashboard.CurrentProgram.CoachId != GetUserId())
                return Forbid();
            return Ok(dashboard);
        }
    }
}
