using System.Security.Claims;
using H2_Trainning.Dtos;
using H2_Trainning.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H2_Trainning.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Coach")]
    public class ProgramsController : ControllerBase
    {
        private readonly IProgramService _service;

        public ProgramsController(IProgramService service)
        {
            _service = service;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var programs = await _service.GetByCoachIdAsync(GetUserId());
            return Ok(programs);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummaries()
        {
            var programs = await _service.GetSummariesByCoachIdAsync(GetUserId());
            return Ok(programs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var program = await _service.GetByIdAsync(id);
            if (program == null) return NotFound();
            return Ok(program);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProgramDto dto)
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProgramDto dto)
        {
            var result = await _service.UpdateAsync(id, GetUserId(), dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}/media")]
        public async Task<IActionResult> SaveMedia(int id, [FromBody] SaveProgramMediaDto dto)
        {
            var expectedPrefix = dto.Target == ProgramMediaTarget.Exercise
                ? "data:"
                : "data:image/";

            if (!dto.DataUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !dto.DataUrl.Contains(";base64,", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "The selected media file is invalid." });
            }

            if (dto.Target == ProgramMediaTarget.Exercise &&
                !dto.DataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) &&
                !dto.DataUrl.StartsWith("data:video/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Exercise media must be an image or video." });
            }

            if (dto.Target == ProgramMediaTarget.Exercise)
            {
                dto.MediaType = dto.DataUrl.StartsWith("data:video/", StringComparison.OrdinalIgnoreCase)
                    ? "Video"
                    : "Image";
            }

            var saved = await _service.SaveMediaAsync(id, GetUserId(), dto);
            if (!saved) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/duplicate")]
        public async Task<IActionResult> Duplicate(int id)
        {
            var result = await _service.DuplicateAsync(id, GetUserId());
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id, GetUserId());
            if (!deleted) return NotFound();
            return NoContent();
        }

    }
}
