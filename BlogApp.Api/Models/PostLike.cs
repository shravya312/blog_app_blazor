namespace BlogApp.Api.Models;

public class PostLike
{
    public int Id { get; set; }
    
    public int PostId { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Post Post { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
