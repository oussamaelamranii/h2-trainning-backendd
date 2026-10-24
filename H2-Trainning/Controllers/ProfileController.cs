using System.Security.Claims;
using H2_Trainning.Dtos;
using H2_Trainning.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H2_Trainning.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var profile = await _service.GetAsync(UserId, UserId, User.IsInRole("Coach"));
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpGet("clients/{clientId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetClient(string clientId)
        {
            var profile = await _service.GetAsync(UserId, clientId, true);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var profile = await _service.UpdateAsync(UserId, dto);
                return profile == null ? NotFound() : Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("clients/{clientId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> UpdateClient(string clientId, UpdateProfileDto dto)
        {
            try
            {
                var profile = await _service.UpdateClientAsync(UserId, clientId, dto);
                return profile == null ? NotFound() : Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
