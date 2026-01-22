using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;
using BlogApp.Api.Services;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TagsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetTags()
    {
        var tags = await _context.Tags
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                PostCount = t.PostTags.Count(pt => pt.Post.Status == PostStatus.Published && pt.Post.PublishedAt <= DateTime.UtcNow)
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.PostTags)
                .ThenInclude(pt => pt.Post)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null)
            return NotFound();

        return Ok(new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            PostCount = tag.PostTags.Count(pt => pt.Post.Status == PostStatus.Published && pt.Post.PublishedAt <= DateTime.UtcNow)
        });
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<TagDto>> GetTagBySlug(string slug)
    {
        var tag = await _context.Tags
            .Include(t => t.PostTags)
                .ThenInclude(pt => pt.Post)
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tag == null)
            return NotFound();

        return Ok(new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            PostCount = tag.PostTags.Count(pt => pt.Post.Status == PostStatus.Published && pt.Post.PublishedAt <= DateTime.UtcNow)
        });
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Editor,Author")]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        var slug = string.IsNullOrEmpty(dto.Slug) 
            ? SlugService.GenerateSlug(dto.Name) 
            : SlugService.GenerateSlug(dto.Slug);

        var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
        if (existingTag != null)
            return Ok(new TagDto
            {
                Id = existingTag.Id,
                Name = existingTag.Name,
                Slug = existingTag.Slug,
                PostCount = await _context.PostTags.CountAsync(pt => pt.TagId == existingTag.Id)
            });

        var tag = new Tag
        {
            Name = dto.Name,
            Slug = slug
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            PostCount = 0
        });
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
            return NotFound();

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
