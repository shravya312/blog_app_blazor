using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string SupabaseUserId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    [MaxLength(1000)]
    public string? Bio { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    [MaxLength(200)]
    public string? Twitter { get; set; }
    
    [MaxLength(200)]
    public string? LinkedIn { get; set; }
    
    [MaxLength(200)]
    public string? GitHub { get; set; }
    
    public UserRole Role { get; set; } = UserRole.Reader;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsEmailVerified { get; set; }
    
    // Navigation properties
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
}

public enum UserRole
{
    Reader = 0,
    Author = 1,
    Editor = 2,
    Admin = 3
}
