namespace BlogApp.Api.Models;

public class PostView
{
    public int Id { get; set; }
    
    public int PostId { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Post Post { get; set; } = null!;
}
