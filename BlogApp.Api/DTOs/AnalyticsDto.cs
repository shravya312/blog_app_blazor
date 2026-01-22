namespace BlogApp.Api.DTOs;

public class AnalyticsDto
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int TotalUsers { get; set; }
    public int TotalComments { get; set; }
    public int PendingComments { get; set; }
    public int TotalViews { get; set; }
    public int TotalLikes { get; set; }
    public int NewsletterSubscribers { get; set; }
    public List<PostViewsByDateDto> ViewsByDate { get; set; } = new();
    public List<PopularPostDto> PopularPosts { get; set; } = new();
}

public class PostViewsByDateDto
{
    public DateTime Date { get; set; }
    public int ViewCount { get; set; }
}

public class PopularPostDto
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
}
