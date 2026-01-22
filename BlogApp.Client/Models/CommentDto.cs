namespace BlogApp.Client.Models;

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public UserDto? Author { get; set; }
    public int? ParentCommentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}
