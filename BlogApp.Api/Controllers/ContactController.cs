using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.Models;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult> SendMessage([FromBody] ContactMessageDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Message))
            return BadRequest("Name, email, and message are required");

        var message = new ContactMessage
        {
            Name = dto.Name,
            Email = dto.Email,
            Subject = dto.Subject,
            Message = dto.Message,
            IsRead = false
        };

        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Message sent successfully", id = message.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<object>>> GetMessages([FromQuery] bool unreadOnly = false)
    {
        var query = _context.ContactMessages.AsQueryable();

        if (unreadOnly)
            query = query.Where(m => !m.IsRead);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Email,
                m.Subject,
                m.Message,
                m.IsRead,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPut("{id}/read")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        message.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class ContactMessageDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
