using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models;

public class NewsletterSubscription
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UnsubscribedAt { get; set; }
}
