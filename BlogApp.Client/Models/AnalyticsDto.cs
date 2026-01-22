namespace BlogApp.Client.Models;

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
}
