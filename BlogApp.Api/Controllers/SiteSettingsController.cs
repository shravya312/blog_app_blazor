using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SiteSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SiteSettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<SiteSettingsDto>> GetSettings()
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(new SiteSettingsDto
        {
            SiteTitle = settings.SiteTitle,
            SiteDescription = settings.SiteDescription,
            SiteUrl = settings.SiteUrl,
            LogoUrl = settings.LogoUrl,
            Theme = settings.Theme,
            CommentsEnabled = settings.CommentsEnabled,
            NewsletterEnabled = settings.NewsletterEnabled
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SiteSettingsDto>> UpdateSettings([FromBody] UpdateSiteSettingsDto dto)
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
        }

        if (dto.SiteTitle != null)
            settings.SiteTitle = dto.SiteTitle;
        if (dto.SiteDescription != null)
            settings.SiteDescription = dto.SiteDescription;
        if (dto.SiteUrl != null)
            settings.SiteUrl = dto.SiteUrl;
        if (dto.LogoUrl != null)
            settings.LogoUrl = dto.LogoUrl;
        if (dto.Theme != null)
            settings.Theme = dto.Theme;
        if (dto.CommentsEnabled.HasValue)
            settings.CommentsEnabled = dto.CommentsEnabled.Value;
        if (dto.NewsletterEnabled.HasValue)
            settings.NewsletterEnabled = dto.NewsletterEnabled.Value;

        settings.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new SiteSettingsDto
        {
            SiteTitle = settings.SiteTitle,
            SiteDescription = settings.SiteDescription,
            SiteUrl = settings.SiteUrl,
            LogoUrl = settings.LogoUrl,
            Theme = settings.Theme,
            CommentsEnabled = settings.CommentsEnabled,
            NewsletterEnabled = settings.NewsletterEnabled
        });
    }
}
