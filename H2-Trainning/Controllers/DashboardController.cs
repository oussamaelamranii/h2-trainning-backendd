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
            // The coach view needs progress and check-ins, not the original
            // base64 media blobs stored inside program templates.
            var dashboard = await _service.GetClientDashboardAsync(clientId, includeMedia: false);
            if (dashboard?.CurrentProgram != null && dashboard.CurrentProgram.CoachId != GetUserId())
                return Forbid();
            return Ok(dashboard);
        }
    }
}
