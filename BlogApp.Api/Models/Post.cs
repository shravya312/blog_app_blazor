using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Api.Models;

public class Post
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "ntext")]
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Excerpt { get; set; }
    
    [MaxLength(500)]
    public string? FeaturedImageUrl { get; set; }
    
    [MaxLength(200)]
    public string? MetaTitle { get; set; }
    
    [MaxLength(500)]
    public string? MetaDescription { get; set; }
    
    public PostStatus Status { get; set; } = PostStatus.Draft;
    
    public int AuthorId { get; set; }
    
    public int? CategoryId { get; set; }
    
    public DateTime? PublishedAt { get; set; }
    
    public DateTime? ScheduledFor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int ViewCount { get; set; } = 0;
    
    public int LikeCount { get; set; } = 0;
    
    public int CommentCount { get; set; } = 0;
    
    // Navigation properties
    public virtual User Author { get; set; } = null!;
    public virtual Category? Category { get; set; }
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
}

public enum PostStatus
{
    Draft = 0,
    Published = 1,
    Scheduled = 2,
    Archived = 3
}
