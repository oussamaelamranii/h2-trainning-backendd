using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using H2_Trainning.Data;
using H2_Trainning.Enums;
using H2_Trainning.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Controllers;

[ApiController, Route("api/communication"), Authorize]
public class CommunicationController(ApplicationDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public class SendMessageDto
    {
        [Required, MaxLength(2000)] public string Content { get; set; } = string.Empty;
    }

    [HttpGet("clients/{clientId}/messages"), Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetClientMessages(string clientId)
    {
        if (!await OwnsClient(clientId)) return NotFound();
        return Ok(await Messages(clientId, UserId).ToListAsync());
    }

    [HttpPost("clients/{clientId}/messages"), Authorize(Roles = "Coach")]
    public async Task<IActionResult> Send(string clientId, SendMessageDto dto)
    {
        if (!await OwnsClient(clientId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { message = "Message cannot be empty." });
        var message = new CoachMessage { CoachId = UserId, ClientId = clientId, Content = dto.Content.Trim() };
        db.CoachMessages.Add(message);
        await db.SaveChangesAsync();
        var coachName = await db.Users.Where(u => u.Id == UserId).Select(u => u.FullName).FirstAsync();
        return Ok(new MessageDto(message.Id, message.CoachId, coachName, message.Content, message.CreatedAt, message.ReadAt));
    }

    [HttpGet("messages"), Authorize(Roles = "Client")]
    public async Task<IActionResult> GetMessages() => Ok(await Messages(UserId).ToListAsync());

    [HttpPost("messages/{id:int}/read"), Authorize(Roles = "Client")]
    public async Task<IActionResult> ReadMessage(int id)
    {
        await db.CoachMessages.Where(m => m.Id == id && m.ClientId == UserId && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, DateTime.UtcNow));
        return NoContent();
    }

    [HttpGet("notifications"), Authorize(Roles = "Client")]
    public async Task<IActionResult> Notifications()
    {
        var coachId = await db.Users.Where(u => u.Id == UserId).Select(u => u.CoachId).FirstOrDefaultAsync();
        var unreadPostIds = await db.Posts.Where(p => p.CoachId == coachId
            && !db.PostReads.Any(r => r.ClientId == UserId && r.PostId == p.Id)).Select(p => p.Id).ToListAsync();
        var unreadMessages = await db.CoachMessages.CountAsync(m => m.ClientId == UserId && m.ReadAt == null);
        return Ok(new { unreadPostIds, unreadMessages });
    }

    [HttpPost("posts/{id:int}/read"), Authorize(Roles = "Client")]
    public async Task<IActionResult> ReadPost(int id)
    {
        var coachId = await db.Users.Where(u => u.Id == UserId).Select(u => u.CoachId).FirstOrDefaultAsync();
        if (!await db.Posts.AnyAsync(p => p.Id == id && p.CoachId == coachId)) return NotFound();
        // Serialize concurrent requests for the same receipt (e.g. two open tabs).
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO PostReads (ClientId, PostId, ReadAt)
            SELECT {UserId}, {id}, {DateTime.UtcNow}
            WHERE NOT EXISTS (SELECT 1 FROM PostReads WITH (UPDLOCK, HOLDLOCK) WHERE ClientId = {UserId} AND PostId = {id})");
        return NoContent();
    }

    private Task<bool> OwnsClient(string id) => db.Users.AnyAsync(u => u.Id == id && u.CoachId == UserId && u.Role == Role.Client);

    private IQueryable<MessageDto> Messages(string clientId, string? coachId = null) => db.CoachMessages.AsNoTracking()
        .Where(m => m.ClientId == clientId && (coachId == null || m.CoachId == coachId)).OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
        .Select(m => new MessageDto(m.Id, m.CoachId, m.Coach.FullName, m.Content, m.CreatedAt, m.ReadAt));

    public record MessageDto(int Id, string CoachId, string CoachName, string Content, DateTime CreatedAt, DateTime? ReadAt);
}
