using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models;

public class SiteSettings
{
    public int Id { get; set; }
    
    [MaxLength(200)]
    public string SiteTitle { get; set; } = "My Blog";
    
    [MaxLength(500)]
    public string? SiteDescription { get; set; }
    
    [MaxLength(200)]
    public string? SiteUrl { get; set; }
    
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    
    [MaxLength(100)]
    public string Theme { get; set; } = "light";
    
    public bool CommentsEnabled { get; set; } = true;
    
    public bool NewsletterEnabled { get; set; } = true;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
