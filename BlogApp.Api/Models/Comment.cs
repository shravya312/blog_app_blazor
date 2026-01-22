using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Api.Models;

public class Comment
{
    public int Id { get; set; }
    
    [Required]
    [Column(TypeName = "ntext")]
    public string Content { get; set; } = string.Empty;
    
    public int PostId { get; set; }
    
    public int AuthorId { get; set; }
    
    public int? ParentCommentId { get; set; }
    
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Post Post { get; set; } = null!;
    public virtual User Author { get; set; } = null!;
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}

public enum CommentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
