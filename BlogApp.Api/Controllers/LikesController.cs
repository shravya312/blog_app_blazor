using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.Models;
using BlogApp.Api.Services;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseService _supabaseService;

    public LikesController(ApplicationDbContext context, ISupabaseService supabaseService)
    {
        _context = context;
        _supabaseService = supabaseService;
    }

    [HttpPost("post/{postId}")]
    public async Task<ActionResult> ToggleLike(int postId)
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null)
            return Unauthorized();

        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return NotFound();

        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == user.Id);

        if (existingLike != null)
        {
            _context.PostLikes.Remove(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }
        else
        {
            _context.PostLikes.Add(new PostLike
            {
                PostId = postId,
                UserId = user.Id
            });
            post.LikeCount++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { isLiked = existingLike == null, likeCount = post.LikeCount });
    }
}
