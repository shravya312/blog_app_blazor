using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;
using BlogApp.Api.Services;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                PostCount = c.Posts.Count(p => p.Status == PostStatus.Published && p.PublishedAt <= DateTime.UtcNow)
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Posts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        return Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            PostCount = category.Posts.Count(p => p.Status == PostStatus.Published && p.PublishedAt <= DateTime.UtcNow)
        });
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug)
    {
        var category = await _context.Categories
            .Include(c => c.Posts)
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (category == null)
            return NotFound();

        return Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            PostCount = category.Posts.Count(p => p.Status == PostStatus.Published && p.PublishedAt <= DateTime.UtcNow)
        });
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var slug = string.IsNullOrEmpty(dto.Slug) 
            ? SlugService.GenerateSlug(dto.Name) 
            : SlugService.GenerateSlug(dto.Slug);

        var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (existingCategory != null)
            return Conflict("Category with this slug already exists");

        var category = new Category
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            PostCount = 0
        });
    }

    [HttpPut("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CreateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        category.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Slug))
        {
            var slug = SlugService.GenerateSlug(dto.Slug);
            if (slug != category.Slug)
            {
                var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug && c.Id != id);
                if (existingCategory == null)
                    category.Slug = slug;
            }
        }
        category.Description = dto.Description;

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            PostCount = await _context.Posts.CountAsync(p => p.CategoryId == id && p.Status == PostStatus.Published)
        });
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
