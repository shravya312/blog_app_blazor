namespace BlogApp.Api.DTOs;

public class SiteSettingsDto
{
    public string SiteTitle { get; set; } = "My Blog";
    public string? SiteDescription { get; set; }
    public string? SiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string Theme { get; set; } = "light";
    public bool CommentsEnabled { get; set; } = true;
    public bool NewsletterEnabled { get; set; } = true;
}

public class UpdateSiteSettingsDto
{
    public string? SiteTitle { get; set; }
    public string? SiteDescription { get; set; }
    public string? SiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? Theme { get; set; }
    public bool? CommentsEnabled { get; set; }
    public bool? NewsletterEnabled { get; set; }
}
