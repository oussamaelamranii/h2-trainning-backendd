using System.Security.Claims;
using H2_Trainning.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H2_Trainning.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Coach")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IProgressAnalyticsService _service;

        public AnalyticsController(IProgressAnalyticsService service)
        {
            _service = service;
        }

        [HttpGet("clients/{clientId}")]
        public async Task<IActionResult> GetClientProgress(string clientId, [FromQuery] int weeks = 6, [FromQuery] int? assignmentId = null)
        {
            var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var report = await _service.GetClientProgressAsync(coachId, clientId, weeks, assignmentId);
            if (report == null) return NotFound(new { message = "Client not found." });
            return Ok(report);
        }

        [HttpGet("clients/{clientId}/programs")]
        public async Task<IActionResult> GetClientProgramHistory(string clientId)
        {
            var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var history = await _service.GetClientProgramHistoryAsync(coachId, clientId);
            if (history == null) return NotFound(new { message = "Client not found." });
            return Ok(history);
        }
    }
}
