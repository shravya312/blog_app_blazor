using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var analytics = new AnalyticsDto
        {
            TotalPosts = await _context.Posts.CountAsync(),
            PublishedPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Published),
            DraftPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Draft),
            TotalUsers = await _context.Users.CountAsync(),
            TotalComments = await _context.Comments.CountAsync(c => c.Status == CommentStatus.Approved),
            PendingComments = await _context.Comments.CountAsync(c => c.Status == CommentStatus.Pending),
            TotalViews = await _context.PostViews.CountAsync(),
            TotalLikes = await _context.PostLikes.CountAsync(),
            NewsletterSubscribers = await _context.NewsletterSubscriptions.CountAsync(n => n.IsActive),
            ViewsByDate = await _context.PostViews
                .Where(pv => pv.ViewedAt >= startDate)
                .GroupBy(pv => pv.ViewedAt.Date)
                .Select(g => new PostViewsByDateDto
                {
                    Date = g.Key,
                    ViewCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync(),
            PopularPosts = await _context.Posts
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.ViewCount)
                .Take(10)
                .Select(p => new PopularPostDto
                {
                    PostId = p.Id,
                    Title = p.Title,
                    ViewCount = p.ViewCount,
                    LikeCount = p.LikeCount
                })
                .ToListAsync()
        };

        return Ok(analytics);
    }
}
