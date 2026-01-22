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
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseService _supabaseService;
    private readonly ReadingTimeService _readingTimeService;

    public PostsController(
        ApplicationDbContext context,
        ISupabaseService supabaseService,
        ReadingTimeService readingTimeService)
    {
        _context = context;
        _supabaseService = supabaseService;
        _readingTimeService = readingTimeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PostSummaryDto>>> GetPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        // Filter by status (only published for non-authenticated users)
        var supabaseUserId = await GetCurrentUserId();
        if (string.IsNullOrEmpty(supabaseUserId) || !await IsAdminOrEditor(supabaseUserId))
        {
            query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt <= DateTime.UtcNow);
        }
        else if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<PostStatus>(status, out var postStatus))
            {
                query = query.Where(p => p.Status == postStatus);
            }
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == category);
        }

        // Filter by tag
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(p => p.PostTags.Any(pt => pt.Tag.Slug == tag));
        }

        // Search
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Title.Contains(search) || 
                                   p.Content.Contains(search) || 
                                   p.Excerpt != null && p.Excerpt.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var posts = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var postDtos = posts.Select(p => MapToSummaryDto(p, supabaseUserId)).ToList();

        return Ok(new PagedResult<PostSummaryDto>
        {
            Items = postDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PostDto>> GetPost(int id)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound();

        // Check if user can view this post
        var supabaseUserId = await GetCurrentUserId();
        if (post.Status != PostStatus.Published && 
            (string.IsNullOrEmpty(supabaseUserId) || post.Author.SupabaseUserId != supabaseUserId))
        {
            return NotFound();
        }

        // Increment view count
        post.ViewCount++;
        await _context.SaveChangesAsync();

        // Track view
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        _context.PostViews.Add(new PostView
        {
            PostId = post.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ViewedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(MapToDto(post, supabaseUserId));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<PostDto>> GetPostBySlug(string slug)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (post == null)
            return NotFound();

        var supabaseUserId = await GetCurrentUserId();
        if (post.Status != PostStatus.Published && 
            (string.IsNullOrEmpty(supabaseUserId) || post.Author.SupabaseUserId != supabaseUserId))
        {
            return NotFound();
        }

        post.ViewCount++;
        await _context.SaveChangesAsync();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        _context.PostViews.Add(new PostView
        {
            PostId = post.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ViewedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(MapToDto(post, supabaseUserId));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto)
    {
        var supabaseUserId = await GetCurrentUserId();
        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null)
            return Unauthorized();

        if (user.Role < UserRole.Author)
            return Forbid("Insufficient permissions");

        var slug = string.IsNullOrEmpty(dto.Slug) 
            ? SlugService.GenerateSlug(dto.Title) 
            : SlugService.GenerateSlug(dto.Slug);

        // Ensure unique slug
        var existingPost = await _context.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        if (existingPost != null)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks}";
        }

        if (!Enum.TryParse<PostStatus>(dto.Status, out var status))
            status = PostStatus.Draft;

        var post = new Post
        {
            Title = dto.Title,
            Slug = slug,
            Content = dto.Content,
            Excerpt = dto.Excerpt,
            FeaturedImageUrl = dto.FeaturedImageUrl,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            Status = status,
            AuthorId = user.Id,
            CategoryId = dto.CategoryId,
            ScheduledFor = dto.ScheduledFor,
            PublishedAt = status == PostStatus.Published ? DateTime.UtcNow : null
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Add tags
        if (dto.TagIds.Any())
        {
            foreach (var tagId in dto.TagIds)
            {
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag != null)
                {
                    _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
                }
            }
            await _context.SaveChangesAsync();
        }

        await _context.Entry(post).Reference(p => p.Author).LoadAsync();
        await _context.Entry(post).Reference(p => p.Category).LoadAsync();
        await _context.Entry(post).Collection(p => p.PostTags).Query().Include(pt => pt.Tag).LoadAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, MapToDto(post, supabaseUserId));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<PostDto>> UpdatePost(int id, [FromBody] UpdatePostDto dto)
    {
        var supabaseUserId = await GetCurrentUserId();
        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null || (post.AuthorId != user.Id && user.Role < UserRole.Editor))
            return Forbid();

        post.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug))
        {
            var slug = SlugService.GenerateSlug(dto.Slug);
            if (slug != post.Slug)
            {
                var existingPost = await _context.Posts.FirstOrDefaultAsync(p => p.Slug == slug && p.Id != id);
                if (existingPost == null)
                    post.Slug = slug;
            }
        }
        post.Content = dto.Content;
        post.Excerpt = dto.Excerpt;
        post.FeaturedImageUrl = dto.FeaturedImageUrl;
        post.MetaTitle = dto.MetaTitle;
        post.MetaDescription = dto.MetaDescription;
        post.CategoryId = dto.CategoryId;
        post.ScheduledFor = dto.ScheduledFor;
        post.UpdatedAt = DateTime.UtcNow;

        if (Enum.TryParse<PostStatus>(dto.Status, out var status))
        {
            post.Status = status;
            if (status == PostStatus.Published && post.PublishedAt == null)
                post.PublishedAt = DateTime.UtcNow;
        }

        // Update tags
        _context.PostTags.RemoveRange(post.PostTags);
        if (dto.TagIds.Any())
        {
            foreach (var tagId in dto.TagIds)
            {
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag != null)
                {
                    _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
                }
            }
        }

        await _context.SaveChangesAsync();

        await _context.Entry(post).Reference(p => p.Author).LoadAsync();
        await _context.Entry(post).Reference(p => p.Category).LoadAsync();
        await _context.Entry(post).Collection(p => p.PostTags).Query().Include(pt => pt.Tag).LoadAsync();

        return Ok(MapToDto(post, supabaseUserId));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        var supabaseUserId = await GetCurrentUserId();
        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var post = await _context.Posts.FindAsync(id);
        if (post == null)
            return NotFound();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null || (post.AuthorId != user.Id && user.Role < UserRole.Editor))
            return Forbid();

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/related")]
    public async Task<ActionResult<List<PostSummaryDto>>> GetRelatedPosts(int id, [FromQuery] int count = 5)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound();

        var relatedPosts = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.Id != id && 
                       p.Status == PostStatus.Published && 
                       p.PublishedAt <= DateTime.UtcNow &&
                       (p.CategoryId == post.CategoryId || 
                        p.PostTags.Any(pt => post.PostTags.Select(opt => opt.TagId).Contains(pt.TagId))))
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync();

        var supabaseUserId = await GetCurrentUserId();
        return Ok(relatedPosts.Select(p => MapToSummaryDto(p, supabaseUserId)).ToList());
    }

    private async Task<string?> GetCurrentUserId()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Replace("Bearer ", "");
        return await _supabaseService.GetUserIdFromTokenAsync(token);
    }

    private async Task<bool> IsAdminOrEditor(string supabaseUserId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);
        return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Editor);
    }

    private PostDto MapToDto(Post post, string? supabaseUserId)
    {
        var isLiked = false;
        if (!string.IsNullOrEmpty(supabaseUserId))
        {
            var user = _context.Users.FirstOrDefault(u => u.SupabaseUserId == supabaseUserId);
            if (user != null)
            {
                isLiked = _context.PostLikes.Any(pl => pl.PostId == post.Id && pl.UserId == user.Id);
            }
        }

        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            Excerpt = post.Excerpt,
            FeaturedImageUrl = post.FeaturedImageUrl,
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            Status = post.Status.ToString(),
            AuthorId = post.AuthorId,
            Author = post.Author != null ? new UserDto
            {
                Id = post.Author.Id,
                Username = post.Author.Username,
                AvatarUrl = post.Author.AvatarUrl
            } : null,
            CategoryId = post.CategoryId,
            Category = post.Category != null ? new CategoryDto
            {
                Id = post.Category.Id,
                Name = post.Category.Name,
                Slug = post.Category.Slug
            } : null,
            Tags = post.PostTags.Select(pt => new TagDto
            {
                Id = pt.Tag.Id,
                Name = pt.Tag.Name,
                Slug = pt.Tag.Slug
            }).ToList(),
            PublishedAt = post.PublishedAt,
            ScheduledFor = post.ScheduledFor,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            ViewCount = post.ViewCount,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            IsLiked = isLiked,
            ReadingTimeMinutes = _readingTimeService.CalculateReadingTime(post.Content)
        };
    }

    private PostSummaryDto MapToSummaryDto(Post post, string? supabaseUserId)
    {
        return new PostSummaryDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.Excerpt,
            FeaturedImageUrl = post.FeaturedImageUrl,
            AuthorId = post.AuthorId,
            Author = post.Author != null ? new UserDto
            {
                Id = post.Author.Id,
                Username = post.Author.Username,
                AvatarUrl = post.Author.AvatarUrl
            } : null,
            CategoryId = post.CategoryId,
            Category = post.Category != null ? new CategoryDto
            {
                Id = post.Category.Id,
                Name = post.Category.Name,
                Slug = post.Category.Slug
            } : null,
            Tags = post.PostTags.Select(pt => new TagDto
            {
                Id = pt.Tag.Id,
                Name = pt.Tag.Name,
                Slug = pt.Tag.Slug
            }).ToList(),
            PublishedAt = post.PublishedAt,
            ViewCount = post.ViewCount,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            ReadingTimeMinutes = _readingTimeService.CalculateReadingTime(post.Content)
        };
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
