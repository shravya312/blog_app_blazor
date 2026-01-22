using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.Models;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NewsletterController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("subscribe")]
    public async Task<ActionResult> Subscribe([FromBody] string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return BadRequest("Invalid email address");

        var existing = await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == email);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UnsubscribedAt = null;
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Already subscribed" });
        }

        var subscription = new NewsletterSubscription
        {
            Email = email,
            IsActive = true
        };

        _context.NewsletterSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscribed successfully" });
    }

    [HttpPost("unsubscribe")]
    public async Task<ActionResult> Unsubscribe([FromBody] string email)
    {
        var subscription = await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == email);

        if (subscription == null)
            return NotFound();

        subscription.IsActive = false;
        subscription.UnsubscribedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Unsubscribed successfully" });
    }

    [HttpGet("subscribers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<object>>> GetSubscribers()
    {
        var subscribers = await _context.NewsletterSubscriptions
            .Where(n => n.IsActive)
            .Select(n => new { n.Id, n.Email, n.SubscribedAt })
            .ToListAsync();

        return Ok(subscribers);
    }
}
