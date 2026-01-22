using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;
using BlogApp.Api.Services;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseService _supabaseService;

    public CommentsController(ApplicationDbContext context, ISupabaseService supabaseService)
    {
        _context = context;
        _supabaseService = supabaseService;
    }

    [HttpGet("post/{postId}")]
    public async Task<ActionResult<List<CommentDto>>> GetPostComments(int postId)
    {
        var comments = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Where(c => c.PostId == postId && 
                       c.ParentCommentId == null && 
                       c.Status == CommentStatus.Approved)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var commentDtos = comments.Select(c => MapToDto(c)).ToList();
        return Ok(commentDtos);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentDto dto)
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null)
            return Unauthorized();

        var post = await _context.Posts.FindAsync(dto.PostId);
        if (post == null)
            return NotFound("Post not found");

        if (dto.ParentCommentId.HasValue)
        {
            var parentComment = await _context.Comments.FindAsync(dto.ParentCommentId.Value);
            if (parentComment == null || parentComment.PostId != dto.PostId)
                return BadRequest("Invalid parent comment");
        }

        var comment = new Comment
        {
            Content = dto.Content,
            PostId = dto.PostId,
            AuthorId = user.Id,
            ParentCommentId = dto.ParentCommentId,
            Status = user.Role >= UserRole.Editor ? CommentStatus.Approved : CommentStatus.Pending
        };

        _context.Comments.Add(comment);
        
        // Update post comment count
        post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == dto.PostId && c.Status == CommentStatus.Approved);
        
        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
        if (comment.ParentCommentId.HasValue)
            await _context.Entry(comment).Reference(c => c.ParentComment).LoadAsync();

        return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, MapToDto(comment));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CommentDto>> GetComment(int id)
    {
        var comment = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
            return NotFound();

        return Ok(MapToDto(comment));
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult> ApproveComment(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null)
            return NotFound();

        comment.Status = CommentStatus.Approved;
        await _context.SaveChangesAsync();

        // Update post comment count
        var post = await _context.Posts.FindAsync(comment.PostId);
        if (post != null)
        {
            post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == comment.PostId && c.Status == CommentStatus.Approved);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult> RejectComment(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null)
            return NotFound();

        comment.Status = CommentStatus.Rejected;
        await _context.SaveChangesAsync();

        // Update post comment count
        var post = await _context.Posts.FindAsync(comment.PostId);
        if (post != null)
        {
            post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == comment.PostId && c.Status == CommentStatus.Approved);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var comment = await _context.Comments.FindAsync(id);
        if (comment == null)
            return NotFound();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null || (comment.AuthorId != user.Id && user.Role < UserRole.Editor))
            return Forbid();

        _context.Comments.Remove(comment);
        
        // Update post comment count
        var post = await _context.Posts.FindAsync(comment.PostId);
        if (post != null)
        {
            post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == comment.PostId && c.Status == CommentStatus.Approved);
        }
        
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<List<CommentDto>>> GetPendingComments()
    {
        var comments = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => c.Status == CommentStatus.Pending)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(MapToDto));
    }

    private CommentDto MapToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            Author = comment.Author != null ? new UserDto
            {
                Id = comment.Author.Id,
                Username = comment.Author.Username,
                AvatarUrl = comment.Author.AvatarUrl
            } : null,
            ParentCommentId = comment.ParentCommentId,
            Status = comment.Status.ToString(),
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Replies = comment.Replies
                .Where(r => r.Status == CommentStatus.Approved)
                .Select(r => MapToDto(r))
                .ToList()
        };
    }
}
